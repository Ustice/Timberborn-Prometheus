using System;
using System.Collections.Generic;
using Timberborn.BlockSystem;
using Timberborn.MapIndexSystem;
using Timberborn.SoilMoistureSystem;
using Timberborn.TerrainSystem;
using Timberborn.WaterSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal sealed class TimberbornEnvironmentAdapter {

    private readonly ITerrainService _terrainService;
    private readonly IBlockService _blockService;
    private readonly IThreadSafeWaterMap _waterMap;
    private readonly ISoilMoistureService _soilMoistureService;
    private readonly MapIndexService _mapIndexService;
    private readonly HashSet<string> _loggedSampleFailures = new();
    private bool _loggedCompatibility;

    public TimberbornEnvironmentAdapter(
      ITerrainService terrainService,
      IBlockService blockService,
      IThreadSafeWaterMap waterMap,
      ISoilMoistureService soilMoistureService,
      MapIndexService mapIndexService) {
      _terrainService = terrainService;
      _blockService = blockService;
      _waterMap = waterMap;
      _soilMoistureService = soilMoistureService;
      _mapIndexService = mapIndexService;
    }

    internal FireGridEnvironmentSample Sample(FireGridCoordinate coordinate) =>
      FireGridEnvironmentSampler.FromWorldSample(coordinate, SampleWorld(coordinate));

    internal FireGridWorldEnvironmentSample SampleWorld(FireGridCoordinate coordinate) {
      LogCompatibility();
      var timberbornCoordinates = ToTimberbornCoordinates(coordinate);
      var terrainAvailable = TrySampleTerrainTopSurface(timberbornCoordinates, out var terrainTopSurfaceY);
      var blockAvailable = TrySampleBlockOccupancy(
        timberbornCoordinates,
        out var hasBlockOccupancy,
        out var hasTopBlockOccupancy);
      var waterAvailable = TrySampleWaterDepth(timberbornCoordinates, out var waterDepth);
      var soilAvailable = TrySampleSoilMoisture(
        coordinate,
        timberbornCoordinates,
        terrainAvailable,
        terrainTopSurfaceY,
        out var soilMoisture);
      var exposedFaceMaskAvailable = terrainAvailable || blockAvailable;
      var exposedFaceMask = exposedFaceMaskAvailable
        ? SampleExposedFaceMask(coordinate)
        : FireGridExposedFaces.All;

      return new FireGridWorldEnvironmentSample(
        terrainAvailable,
        terrainTopSurfaceY,
        blockAvailable,
        hasBlockOccupancy,
        hasTopBlockOccupancy,
        waterAvailable,
        waterDepth,
        soilAvailable,
        soilMoisture,
        exposedFaceMaskAvailable,
        exposedFaceMask);
    }

    private bool TrySampleTerrainTopSurface(Vector3Int timberbornCoordinates, out int terrainTopSurfaceY) {
      terrainTopSurfaceY = 0;
      if (_terrainService is null) {
        return false;
      }

      try {
        terrainTopSurfaceY = _terrainService.GetTerrainHeightBelow(timberbornCoordinates);
        return true;
      } catch (Exception exception) {
        LogSampleFailure("terrain", exception);
        return false;
      }
    }

    private bool TrySampleBlockOccupancy(
      Vector3Int timberbornCoordinates,
      out bool hasBlockOccupancy,
      out bool hasTopBlockOccupancy) {
      hasBlockOccupancy = false;
      hasTopBlockOccupancy = false;
      if (_blockService is null) {
        return false;
      }

      try {
        hasBlockOccupancy = _blockService.AnyObjectAt(timberbornCoordinates);
        hasTopBlockOccupancy = _blockService.AnyTopObjectAt(timberbornCoordinates);
        return true;
      } catch (Exception exception) {
        LogSampleFailure("block_occupancy", exception);
        return false;
      }
    }

    private bool TrySampleWaterDepth(Vector3Int timberbornCoordinates, out float waterDepth) {
      waterDepth = 0f;
      if (_waterMap is null) {
        return false;
      }

      try {
        waterDepth = _waterMap.WaterDepth(timberbornCoordinates);
        return true;
      } catch (Exception exception) {
        LogSampleFailure("water_depth", exception);
        return false;
      }
    }

    private bool TrySampleSoilMoisture(
      FireGridCoordinate coordinate,
      Vector3Int timberbornCoordinates,
      bool terrainAvailable,
      int terrainTopSurfaceY,
      out float soilMoisture) {
      soilMoisture = 0f;
      if (_soilMoistureService is null
        || _mapIndexService is null
        || !FireGridEnvironmentSampler.ShouldSampleSoilMoisture(coordinate, terrainAvailable, terrainTopSurfaceY)) {
        return false;
      }

      try {
        var index = _mapIndexService.CoordinatesToIndex3D(timberbornCoordinates);
        soilMoisture = _soilMoistureService.SoilMoisture(index);
        return true;
      } catch (Exception exception) {
        LogSampleFailure("soil_moisture", exception);
        return false;
      }
    }

    private int SampleExposedFaceMask(FireGridCoordinate coordinate) {
      var mask = FireGridExposedFaces.None;
      if (!IsWorldSolid(new FireGridCoordinate(coordinate.X - 1, coordinate.Y, coordinate.Z))) {
        mask |= FireGridExposedFaces.NegativeX;
      }

      if (!IsWorldSolid(new FireGridCoordinate(coordinate.X + 1, coordinate.Y, coordinate.Z))) {
        mask |= FireGridExposedFaces.PositiveX;
      }

      if (!IsWorldSolid(new FireGridCoordinate(coordinate.X, coordinate.Y - 1, coordinate.Z))) {
        mask |= FireGridExposedFaces.NegativeY;
      }

      if (!IsWorldSolid(new FireGridCoordinate(coordinate.X, coordinate.Y + 1, coordinate.Z))) {
        mask |= FireGridExposedFaces.PositiveY;
      }

      if (!IsWorldSolid(new FireGridCoordinate(coordinate.X, coordinate.Y, coordinate.Z - 1))) {
        mask |= FireGridExposedFaces.NegativeZ;
      }

      if (!IsWorldSolid(new FireGridCoordinate(coordinate.X, coordinate.Y, coordinate.Z + 1))) {
        mask |= FireGridExposedFaces.PositiveZ;
      }

      return mask;
    }

    private bool IsWorldSolid(FireGridCoordinate coordinate) {
      var timberbornCoordinates = ToTimberbornCoordinates(coordinate);
      if (_blockService is not null) {
        try {
          if (_blockService.AnyObjectAt(timberbornCoordinates)) {
            return true;
          }
        } catch {
          return false;
        }
      }

      if (_terrainService is null) {
        return false;
      }

      try {
        return coordinate.Y <= _terrainService.GetTerrainHeightBelow(timberbornCoordinates);
      } catch {
        return false;
      }
    }

    private void LogCompatibility() {
      if (_loggedCompatibility) {
        return;
      }

      _loggedCompatibility = true;
      TimberbornCompatibility.RecordProbe(
        TimberbornCompatibilityArea.Environment,
        _terrainService is not null && _blockService is not null && _waterMap is not null && _soilMoistureService is not null && _mapIndexService is not null,
        "ITerrainService/IBlockService/IThreadSafeWaterMap/ISoilMoistureService/MapIndexService");
    }

    private static Vector3Int ToTimberbornCoordinates(FireGridCoordinate coordinate) =>
      new(coordinate.X, coordinate.Z, coordinate.Y);

    private void LogSampleFailure(string input, Exception exception) {
      var detail = exception.GetType().Name;
      var key = $"{input}:{detail}";
      if (!_loggedSampleFailures.Add(key)) {
        return;
      }

      FireTelemetry.LogWarning($"event=environment_adapter_sample_failed input={input} detail=\"{Escape(detail)}\"");
    }

    private static string Escape(string value) =>
      (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

  }
}
