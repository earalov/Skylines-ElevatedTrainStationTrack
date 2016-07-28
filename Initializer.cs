﻿using System;
using System.Linq;
using UnityEngine;
using MetroOverhaul.SetupPrefab;
using SubwayOverhaul.NEXT;

namespace MetroOverhaul
{
    public class Initializer : AbstractInitializer
    {
        protected override void InitializeImpl()
        {
            var mTunnelInfo = FindOriginalPrefab("Metro Track");
            CreatePrefab("Metro Track Ground", "Train Track", SetupMetroTrack().Apply(mTunnelInfo).Chain(p =>
            {
                CreatePrefab("Metro Track Bridge", "Train Track Bridge", SetupMetroTrack().Apply(mTunnelInfo).Chain(p1 => p.GetComponent<TrainTrackAI>().m_bridgeInfo = p1));
                CreatePrefab("Metro Track Elevated", "Train Track Elevated", SetupMetroTrack().Apply(mTunnelInfo).Chain(p2 => p.GetComponent<TrainTrackAI>().m_elevatedInfo = p2));
                CreatePrefab("Metro Track Slope", "Train Track Slope", SetupMetroTrack().Apply(mTunnelInfo).Chain(p3 => p.GetComponent<TrainTrackAI>().m_slopeInfo = p3));
                CreatePrefab("Metro Track Tunnel", "Train Track Tunnel", SetupMetroTrack().Apply(mTunnelInfo).Chain(p4 => p.GetComponent<TrainTrackAI>().m_tunnelInfo = p4));
                p.GetComponent<TrainTrackAI>().m_connectedElevatedInfo = null;
                p.GetComponent<TrainTrackAI>().m_connectedInfo = null;
            }));
            CreatePrefab("Metro Station Track Ground", "Train Station Track", SetupMetroTrack().Apply(mTunnelInfo));
            CreatePrefab("Metro Station Track Elevated", "Train Station Track", SetupMetroTrack().Apply(mTunnelInfo).Chain(SetupStationTrack()).Chain(SetupElevatedStationTrack()));
            CreatePrefab("Metro Station Track Sunken", "Train Station Track", SetupMetroTrack().Apply(mTunnelInfo).Chain(SetupStationTrack()).Chain(SetupSunkenStationTrack()));
        }

        public static Action<NetInfo> SetupElevatedStationTrack()
        {
            return prefab =>
            {
                var trackAi = prefab.GetComponent<TrainTrackAI>();
                trackAi.m_elevatedInfo = prefab;
            };
        }

        public static Action<NetInfo> SetupSunkenStationTrack()
        {
            return prefab =>
            {
                var trackAi = prefab.GetComponent<TrainTrackAI>();
                trackAi.m_tunnelInfo = prefab;
                prefab.m_maxHeight = -1;
                prefab.m_minHeight = -3;
                prefab.m_lowerTerrain = false;
                prefab.m_class.m_layer = ItemClass.Layer.MetroTunnels | ItemClass.Layer.Default;
            };
        }

        public static Action<NetInfo> SetupStationTrack()
        {
            return prefab =>
            {
                prefab.m_followTerrain = false;
                prefab.m_flattenTerrain = false;
                prefab.m_createGravel = false;
                prefab.m_createPavement = false;
                prefab.m_createRuining = false;
                prefab.m_requireSurfaceMaps = false;
                prefab.m_clipTerrain = false;
                prefab.m_snapBuildingNodes = false;
                prefab.m_placementStyle = ItemClass.Placement.Procedural;
                prefab.m_useFixedHeight = true;
                prefab.m_lowerTerrain = true;
                prefab.m_availableIn = ItemClass.Availability.GameAndAsset;
                prefab.m_intersectClass = null;
            };
        }

        private static Action<NetInfo, NetInfo> SetupMetroTrack()
        {
            var elevatedInfo = FindOriginalPrefab("Basic Road Elevated");
            NetInfo trainTrackInfo = null;
            var version = NetInfoVersion.Ground;
            return (prefab, metroTunnel) =>
            {
                switch (prefab.name)
                {
                    case "Metro Track Elevated":
                        version = NetInfoVersion.Elevated;
                        break;
                    case "Metro Track Slope":
                        version = NetInfoVersion.Slope;
                        break;
                    case "Metro Track Tunnel":
                        version = NetInfoVersion.Tunnel;
                        trainTrackInfo = FindOriginalPrefab("Train Track");
                        break;
                }

                SetupMesh.Setup12mMesh(prefab, version, elevatedInfo, trainTrackInfo);
                SetupTexture.Setup12mTexture(prefab, version);

                var milestone = metroTunnel.GetComponent<MetroTrackAI>().m_createPassMilestone;
                PrefabCollection<VehicleInfo>.FindLoaded("Metro").m_class =
                    ScriptableObject.CreateInstance<ItemClass>();
                prefab.GetComponent<TrainTrackBaseAI>().m_createPassMilestone = milestone;
                prefab.m_class = ScriptableObject.CreateInstance<ItemClass>();
                prefab.m_class.m_subService = ItemClass.SubService.PublicTransportMetro;
                prefab.m_class.m_service = ItemClass.Service.PublicTransport;
                prefab.m_class.m_level = ItemClass.Level.Level1;
                prefab.m_UIPriority = FindOriginalPrefab("Metro Track").m_UIPriority;
                if (prefab.name.Contains("Slope") || prefab.name.Contains("Tunnel"))
                {
                    prefab.m_class.m_layer = ItemClass.Layer.MetroTunnels | ItemClass.Layer.Default;
                    prefab.m_setVehicleFlags = Vehicle.Flags.Transition | Vehicle.Flags.Underground;
                    prefab.m_setCitizenFlags = CitizenInstance.Flags.Transition | CitizenInstance.Flags.Underground;
                }
                else
                {
                    prefab.m_class.m_layer = ItemClass.Layer.Default;
                }
                prefab.m_class.hideFlags = HideFlags.None;
                prefab.m_class.name = prefab.name;
                prefab.m_maxBuildAngle = 90;
                prefab.m_maxTurnAngleCos = Mathf.Cos(prefab.m_maxBuildAngle);
                prefab.m_maxTurnAngle = 60;
                prefab.m_maxTurnAngleCos = Mathf.Cos(prefab.m_maxTurnAngle);
                prefab.m_averageVehicleLaneSpeed = metroTunnel.m_averageVehicleLaneSpeed;
                prefab.m_UnlockMilestone = metroTunnel.m_UnlockMilestone;
                prefab.m_createGravel = false;
                prefab.m_createPavement = false;
                prefab.m_halfWidth = (version == NetInfoVersion.Slope || version == NetInfoVersion.Tunnel) ? 7 : 6;
                prefab.m_isCustomContent = true;
                prefab.m_pavementWidth = 3.5f;

                var speedLimit = metroTunnel.m_lanes.First(l => l.m_vehicleType != VehicleInfo.VehicleType.None).m_speedLimit;

                foreach (var lane in prefab.m_lanes)
                {
                    if (lane.m_vehicleType == VehicleInfo.VehicleType.None)
                    {
                        lane.m_stopType = VehicleInfo.VehicleType.Metro;
                    }
                    else
                    {
                        lane.m_vehicleType = VehicleInfo.VehicleType.Metro;
                        lane.m_speedLimit = speedLimit;
                    }
                }

                Modifiers.RemoveElectricityPoles(prefab);
            };
        }
    }
}
