using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace JHM.MeshBasics;

public sealed partial class HexMapGenerator : Node {
    private struct MapRegion {
        public int xMin, xMax, zMin, zMax;
    }
    private struct ClimateData {
        public float clouds;
        public float moisture;
    }
    public enum HemisphereMode {
        Both, North, South
    }
    struct Biome {
        public int Terrain {get; set; }
        public int Plant {get; set; }

        public Biome(int terrain, int plant) {
            Terrain = terrain;
            Plant = plant;
        }
    }

    private static float[] temperatureBands = { 0.1f, 0.3f, 0.6f };
    private static float[] moistureBands = { 0.12f, 0.28f, 0.85f };
    static Biome[] biomes = {
        new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0),
        new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2),
        new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2),
        new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3)
    };
    private List<ClimateData> _climate = new List<ClimateData>();
    private List<ClimateData> _nextClimate = new List<ClimateData>();
    private List<MapRegion> _regions;
    private Random _rng;
    private int _cellCount;
    private int _landCells;
    private HexCellPriorityQueue _searchFrontier;
    private int _searchFrontierPhase;
    

    [Export] public HexGrid Grid {get; set; }

    [Export] private int _seed = 1337;
    [Export] private bool _staticSeed = false;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _jitterProbability = 0.25f;
    [Export(PropertyHint.Range, "20, 200")] private int _chunkSizeMin = 30;
    [Export(PropertyHint.Range, "20, 200")] private int _chunkSizeMax = 100;
    [Export(PropertyHint.Range, "5, 95")] private int _landPercentage = 50;
    [Export(PropertyHint.Range, "1, 5")] private int _waterLevel = 2;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _highRiseProbability = 0.25f;
    [Export(PropertyHint.Range, "0.0, 0.4")] private float _sinkProbability = 0.2f;
    [Export(PropertyHint.Range, "-4, 0")] private int _elevationMinimum = -2;
    [Export(PropertyHint.Range, "6, 10")] private int _elevationMaximum = 8;
    [Export(PropertyHint.Range, "0, 10")] private int _mapBorderX = 5;
    [Export(PropertyHint.Range, "0, 10")] private int _mapBorderZ = 5;
    [Export(PropertyHint.Range, "0, 10")] private int _regionBorder = 5;
    [Export(PropertyHint.Range, "0, 4")] private int _regionCount = 1;
    [Export(PropertyHint.Range, "0, 100")] private int _erosionPercentage = 50;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _evaporation = 0.5f;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _evaporationFactor = 0.5f;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _precipitationFactor = 0.5f;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _runoffFactor = 0.25f;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _seepageFactor = 0.125f;
    [Export] private HexDirection _windDirection = HexDirection.NW;
    [Export(PropertyHint.Range, "1.0, 10.0")] private float _windStrength = 4.0f;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _startingMoisture = 0.1f;
    [Export(PropertyHint.Range, "0, 20")] private int _riverPercentage = 10;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _extraLakeProbability = 0.25f;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _lowTemperature = 0.0f;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _highTemperature = 1.0f;
    [Export] private HemisphereMode _hemisphere;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _temperatureJitter = 0.1f;


    public void GenerateMap(int x, int z) {
        if (_staticSeed) {
            _rng = new(_seed);
        }
        _cellCount = x * z;
        Grid.CreateMap(x, z);
        if (_searchFrontier == null) {
            _searchFrontier = new HexCellPriorityQueue();
        }
        for (int i = 0; i < _cellCount; i++) {
            Grid.GetCell(i).WaterLevel = _waterLevel;
        }
        CreateRegions();
        CreateLand();
        ErodeLand();
        CreateClimate();
        CreateRivers();
        SetTerrainType();
        for (int i = 0; i < _cellCount; i++) {
            Grid.GetCell(i).SearchPhase = 0;
        }

        
    }

    public override void _EnterTree() {
        _rng = new(_seed);
    }

    private void CreateLand() {
        int landBudget = Mathf.RoundToInt(_cellCount * _landPercentage * 0.01f);
        _landCells = landBudget;
        for (int guard = 0; landBudget > 0 && guard < 10000; guard++) {
            bool sink = _rng.NextDouble() < _sinkProbability;
            for(int i = 0; i < _regions.Count; i++) {
                int chunkSize = (int)_rng.NextInt64(_chunkSizeMin, _chunkSizeMax - 1);
                if (sink) { 
                    landBudget = SinkTerrain(chunkSize, landBudget, _regions[i]);
                }
                else {
                    landBudget = RaiseTerrain(chunkSize + 1, landBudget, _regions[i]);
                    if (landBudget == 0) return;
                }
                // GD.Print(landBudget);
            }
        }
        if (landBudget > 0) {
            GD.PrintErr("Failed to use up " + landBudget + " land budget.");
            _landCells -= landBudget;
        }
    }

    private int RaiseTerrain(int chunkSize, int budget, MapRegion region) {
        _searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell(region);
        firstCell.SearchPhase = _searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        _searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.Coordinates;

        int rise = _rng.NextDouble() < _highRiseProbability ? 2 : 1;
        int size = 0;
        while (size < chunkSize && _searchFrontier.Count > 0) {
            HexCell current = _searchFrontier.Dequeue();
            int originalElevation = current.Elevation;
            int newElevation = originalElevation + rise;
            if (newElevation > _elevationMaximum) {
                continue;
            }
            current.Elevation = newElevation;
            if (originalElevation < _waterLevel &&
                newElevation >= _waterLevel &&
                --budget == 0
            ) {
                break;
            }
            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor != null && neighbor.SearchPhase < _searchFrontierPhase) {
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.Distance = neighbor.Coordinates.DistanceTo(center); ;
                    neighbor.SearchHeuristic = _rng.NextDouble() < _jitterProbability ? 1 : 0;
                    _searchFrontier.Enqueue(neighbor);
                }
            }
        }
        _searchFrontier.Clear();
        return budget;
    }

    private int SinkTerrain(int chunkSize, int budget, MapRegion region) {
        _searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell(region);
        firstCell.SearchPhase = _searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        _searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.Coordinates;

        int sink = _rng.NextDouble() < _highRiseProbability ? 2 : 1;
        int size = 0;
        while (size < chunkSize && _searchFrontier.Count > 0) {
            HexCell current = _searchFrontier.Dequeue();
            int originalElevation = current.Elevation;
            int newElevation = current.Elevation - sink;
            if (newElevation < _elevationMinimum) {
                continue;
            }
            current.Elevation = newElevation;
            if (originalElevation >= _waterLevel &&
                newElevation < _waterLevel
            ) {
                budget += 1;
            }
            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor != null && neighbor.SearchPhase < _searchFrontierPhase) {
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.Distance = neighbor.Coordinates.DistanceTo(center); ;
                    neighbor.SearchHeuristic = _rng.NextDouble() < _jitterProbability ? 1 : 0;
                    _searchFrontier.Enqueue(neighbor);
                }
            }
        }
        _searchFrontier.Clear();
        return budget;
    }

    private HexCell GetRandomCell(MapRegion region) {
        var x = (int)_rng.NextInt64(region.xMin, region.xMax);
        var z = (int)_rng.NextInt64(region.zMin, region.zMax);
        return Grid.GetCell(x, z);
    }

    private void SetTerrainType() {
        temperatureJitterChannel = (int)_rng.Next(0, 4);
        int rockDesertElevation = _elevationMaximum - (_elevationMaximum - _waterLevel) / 2;
        for (int i = 0; i < _cellCount; i++) {
            HexCell cell = Grid.GetCell(i);
            float temperature = DetermineTemperature(cell);
            cell.SetMapData(temperature);
            float moisture = _climate[i].moisture;
            if (!cell.IsUnderwater) {
                int t = 0;
                for (; t < temperatureBands.Length; t++) {
                    if (temperature < temperatureBands[t]) {
                        break;
                    }
                }
                int m = 0;
                for (; m < moistureBands.Length; m++) {
                    if (moisture < moistureBands[m]) {
                        break;
                    }
                }
                Biome cellBiome = biomes[t * 4 + m];

                if (cellBiome.Terrain == 0) {
                    if (cell.Elevation >= rockDesertElevation) {
                        cellBiome.Terrain = 3;
                    }
                }
                else if (cell.Elevation == _elevationMaximum) {
                    cellBiome.Terrain = 4;
                }

                if (cellBiome.Terrain == 4) {
                    cellBiome.Plant = 0;
                }
                else if (cellBiome.Plant < 3 && cell.HasRiver) {
                    cellBiome.Plant += 1;
                }
                cell.TerrainTypeIndex = cellBiome.Terrain;
                cell.PlantLevel = cellBiome.Plant;
            }
            else {
                cell.TerrainTypeIndex = 2;
            }
        }
    }

    private void ErodeLand() {
        List<HexCell> erodibleCells = ListPool<HexCell>.Get();
        for (int i = 0; i < _cellCount; i++) {
            HexCell cell = Grid.GetCell(i);
            if (IsErodible(cell)) {
                erodibleCells.Add(cell);
            }
        }

        int targetErodibleCount = (int)(erodibleCells.Count * (100 - _erosionPercentage) * 0.01f);

        while (erodibleCells.Count > targetErodibleCount) {
            int index = (int)_rng.Next(0, erodibleCells.Count);
            HexCell cell = erodibleCells[index];
            HexCell targetCell = GetErosionTarget(cell);


            cell.Elevation -= 1;
            targetCell.Elevation += 1;
            if (!IsErodible(cell)) {
                erodibleCells[index] = erodibleCells[^1];
                erodibleCells.RemoveAt(erodibleCells.Count - 1);
            }

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = cell.GetNeighbor(d);
                if (
                    neighbor != null && 
                    neighbor.Elevation == cell.Elevation + 2 &&
                    !erodibleCells.Contains(neighbor)
                ) {
                    erodibleCells.Add(neighbor);
                }
            }

            if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell)) {
                erodibleCells.Add(targetCell);
            }

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = targetCell.GetNeighbor(d);
                if (
                    neighbor != null && neighbor != cell &&
                    neighbor.Elevation == targetCell.Elevation + 1 &&
                    !IsErodible(neighbor)
                ) {
                    erodibleCells.Remove(neighbor);
                }
            }
        }


        ListPool<HexCell>.Add(erodibleCells);
    }

    private bool IsErodible(HexCell cell) {
        int erodibleElevation = cell.Elevation - 2;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor != null && neighbor.Elevation <= erodibleElevation) {
                return true;
            }
        }
        return false;
    }

    private HexCell GetErosionTarget(HexCell cell) {
        List<HexCell> candidates = ListPool<HexCell>.Get();
        int erodibleElevation = cell.Elevation - 2;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor != null && neighbor.Elevation <= erodibleElevation) {
                candidates.Add(neighbor);
            }
        }
        HexCell target = candidates[_rng.Next(0, candidates.Count)];
        ListPool<HexCell>.Add(candidates);
        return target;
    }

    private void CreateRegions() {
        if (_regions == null) {
            _regions = new List<MapRegion>();
        }
        else {
            _regions.Clear();
        }

        MapRegion region;
        switch (_regionCount) {
            default:
                region.xMin = _mapBorderX;
                region.xMax = Grid.CellCountX - _mapBorderX;
                region.zMin = _mapBorderZ;
                region.zMax = Grid.CellCountZ - _mapBorderZ;
                _regions.Add(region);
                break;
            case 2:
                if (_rng.NextDouble() < 0.5f) {
                    region.xMin = _mapBorderX;
                    region.xMax = Grid.CellCountX / 2 - _regionBorder;
                    region.zMin = _mapBorderZ;
                    region.zMax = Grid.CellCountZ - _mapBorderZ;
                    _regions.Add(region);
                    region.xMin = Grid.CellCountX / 2 + _regionBorder;
                    region.xMax = Grid.CellCountX - _mapBorderX;
                    _regions.Add(region);
                }
                else {
                    region.xMin = _mapBorderX;
                    region.xMax = Grid.CellCountX - _mapBorderX;
                    region.zMin = _mapBorderZ;
                    region.zMax = Grid.CellCountZ / 2 - _regionBorder;
                    _regions.Add(region);
                    region.zMin = Grid.CellCountZ / 2 + _regionBorder;
                    region.zMax = Grid.CellCountZ - _mapBorderZ;
                    _regions.Add(region);
                }
                break;
            case 3:
                region.xMin = _mapBorderX;
                region.xMax = Grid.CellCountX / 3 - _regionBorder;
                region.zMin = _mapBorderZ;
                region.zMax = Grid.CellCountZ - _mapBorderZ;
                _regions.Add(region);

                region.xMin = Grid.CellCountX / 3 + _regionBorder;
                region.xMax = Grid.CellCountX * 2 / 3 - _regionBorder;
                _regions.Add(region);

                region.xMin = Grid.CellCountX * 2 / 3 + _regionBorder;
                region.xMax = Grid.CellCountX - _mapBorderX;
                _regions.Add(region);
                break;
            case 4:
                region.xMin = _mapBorderX;
                region.xMax = Grid.CellCountX / 2 - _regionBorder;
                region.zMin = _mapBorderZ;
                region.zMax = Grid.CellCountZ / 2 - _regionBorder;
                _regions.Add(region);
                region.xMin = Grid.CellCountX / 2 + _regionBorder;
                region.xMax = Grid.CellCountX - _mapBorderX;
                _regions.Add(region);
                region.zMin = Grid.CellCountZ / 2 + _regionBorder;
                region.zMax = Grid.CellCountZ - _mapBorderZ;
                _regions.Add(region);
                region.xMin = _mapBorderX;
                region.xMax = Grid.CellCountX / 2 - _regionBorder;
                _regions.Add(region);
                break;
        }
    }

    private void CreateClimate() {
        _climate.Clear();
        _nextClimate.Clear();
        ClimateData initialData = new ClimateData();
        initialData.moisture = _startingMoisture;
        ClimateData clearData = new ClimateData();
        for (int i = 0; i < _cellCount; i++) {
            _climate.Add(initialData);
            _nextClimate.Add(clearData);
        }
        for (int cycle = 0; cycle < 40; cycle++) {
            for (int i = 0; i < _cellCount; i++) {
                EvolveClimate(i);
            }
            List<ClimateData> swap = _climate;
            _climate = _nextClimate;
            _nextClimate = swap;
        }
    }

    private void EvolveClimate(int cellIndex) {
        HexCell cell = Grid.GetCell(cellIndex);
        ClimateData cellClimate = _climate[cellIndex];

        if (cell.IsUnderwater) {
            cellClimate.clouds += _evaporation;
            cellClimate.moisture = 1f;
        } else {
            float evaporation = cellClimate.moisture * _evaporationFactor;
            cellClimate.moisture -= evaporation;
            cellClimate.clouds += evaporation;
        }

        float precipitation = cellClimate.clouds * _precipitationFactor;
        cellClimate.clouds -= precipitation;
        cellClimate.moisture += precipitation;
        float cloudMaximum = 1f - cell.ViewElevation / (_elevationMaximum + 1f);
        if (cellClimate.clouds > cloudMaximum) {
            cellClimate.moisture += cellClimate.clouds - cloudMaximum;
            cellClimate.clouds = cloudMaximum;
        }
        HexDirection mainDispersalDirection = _windDirection.Opposite();
        float cloudDispersal = cellClimate.clouds * (1.0f / (5f + _windStrength));
        float runoff = cellClimate.moisture * _runoffFactor * (1f / 6f);
        float seepage = cellClimate.moisture * _seepageFactor * (1f / 6f);
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor is null) {
                continue;
            }
            ClimateData neighborClimate = _nextClimate[neighbor.Index];
            if (d == mainDispersalDirection) {
                neighborClimate.clouds += cloudDispersal * _windStrength;
            }
            else {
                neighborClimate.clouds += cloudDispersal;
            }
            int elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
            if (elevationDelta < 0) {
                cellClimate.moisture -= runoff;
                neighborClimate.moisture += runoff;
            } else if (elevationDelta == 0) {
                cellClimate.moisture -= seepage;
                neighborClimate.moisture += seepage;
            }

            _nextClimate[neighbor.Index] = neighborClimate;
        }
        ClimateData nextCellClimate = _nextClimate[cellIndex];
        nextCellClimate.moisture += cellClimate.moisture;
        if (nextCellClimate.moisture > 1f) {
            nextCellClimate.moisture = 1f;
        }
        _nextClimate[cellIndex] = nextCellClimate;

        _climate[cellIndex] = new ClimateData();
    }

    private void CreateRivers() {
        List<HexCell> riverOrigins = ListPool<HexCell>.Get();
        for (int i = 0; i < _cellCount; i++) {
            HexCell cell = Grid.GetCell(i);
            if (cell.IsUnderwater) {
                continue;
            }
            ClimateData data = _climate[i];
            float weight =
                data.moisture * (cell.Elevation - _waterLevel) /
                (_elevationMaximum - _waterLevel);
            if (weight > 0.75f) {
                riverOrigins.Add(cell);
                riverOrigins.Add(cell);
            }
            if (weight > 0.5f) {
                riverOrigins.Add(cell);
            }
            if (weight > 0.25f) {
                riverOrigins.Add(cell);
            }
        }
        int riverBudget = Mathf.RoundToInt(_landCells * _riverPercentage * 0.01f);
        while (riverBudget > 0 && riverOrigins.Count > 0) {
            int index = (int)_rng.Next(0, riverOrigins.Count);
            int lastIndex = riverOrigins.Count - 1;
            HexCell origin = riverOrigins[index];
            riverOrigins[index] = riverOrigins[lastIndex];
            riverOrigins.RemoveAt(lastIndex);

            if (!origin.HasRiver) {
                bool isValidOrigin = true;
                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                    HexCell neighbor = origin.GetNeighbor(d);
                    if (neighbor != null && (neighbor.HasRiver || neighbor.IsUnderwater)) {
                        isValidOrigin = false;
                        break;
                    }
                }
                if (isValidOrigin) {
                    riverBudget -= CreateRiver(origin);
                }
            }
        }

        if (riverBudget > 0) {
            GD.PrintErr("Failed to use up river budget.");
        }
        ListPool<HexCell>.Add(riverOrigins);
    }

    List<HexDirection> flowDirections = new List<HexDirection>();

    private int CreateRiver(HexCell origin) {
        int length = 1;
        HexCell cell = origin;
        HexDirection direction = HexDirection.NE;
        while (!cell.IsUnderwater) {
            int minNeighborElevation = int.MaxValue;
            flowDirections.Clear();
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = cell.GetNeighbor(d);
                if (neighbor is null) {
                    continue;
                }

                if (neighbor.Elevation < minNeighborElevation) {
                    minNeighborElevation = neighbor.Elevation;
                }

                if (neighbor == origin || neighbor.HasIncomingRiver) {
                    continue;
                }

                int delta = neighbor.Elevation - cell.Elevation;
                
                if (delta > 0) {
                    continue;
                }

                if (neighbor.HasOutgoingRiver) {
                    cell.SetOutgoingRiver(d);
                    return length;
                }

                if (delta < 0) {
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                }

                if (
                    length == 1 ||
                    (d != direction.Next2() && d != direction.Previous2())
                ) {
                    flowDirections.Add(d);
                }

                flowDirections.Add(d);
            }

            if (flowDirections.Count == 0) {
                if (length == 1) {
                    return 0;
                }

                if (minNeighborElevation >= cell.Elevation && _rng.NextDouble() < _extraLakeProbability) {
                    cell.WaterLevel = minNeighborElevation;
                    if (minNeighborElevation == cell.Elevation) {
                        cell.Elevation = minNeighborElevation - 1;
                    }
                }
                break;
            }

            direction = flowDirections[_rng.Next(0, flowDirections.Count)];
            cell.SetOutgoingRiver(direction);
            length += 1;

            if (minNeighborElevation >= cell.Elevation) {
                cell.WaterLevel = cell.Elevation;
                cell.Elevation -= 1;
            }

            cell = cell.GetNeighbor(direction);
        }
        return length;
    }

    int temperatureJitterChannel;
    private float DetermineTemperature(HexCell cell) {
        float latitude = (float)cell.Coordinates.Z / Grid.CellCountZ;
        if (_hemisphere == HemisphereMode.Both) {
            latitude *= 2f;
            if (latitude > 1f) {
                latitude = 2f - latitude;
            }
        }
        else if (_hemisphere == HemisphereMode.North) {
            latitude = 1f - latitude;
        }
        float temperature = Mathf.Lerp(_lowTemperature, _highTemperature, latitude);
        temperature *= 1f - (cell.ViewElevation - _waterLevel) /
            (_elevationMaximum - _waterLevel + 1f);
        float jitter = HexMetrics.SampleNoise(cell.Position * 0.1f)[temperatureJitterChannel];
        temperature += (jitter * 2f - 1f) * _temperatureJitter;
        return temperature;
    }
}
