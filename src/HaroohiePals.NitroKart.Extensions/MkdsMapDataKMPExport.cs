using HaroohiePals.MarioKart.MapData;
using HaroohiePals.NitroKart.MapData;
using HaroohiePals.NitroKart.MapData.Intermediate;
using HaroohiePals.NitroKart.MapData.Intermediate.Sections;
using HaroohiePals.NitroKart.Race;
using Newtonsoft.Json;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static HaroohiePals.NitroKart.MapData.Binary.NkmdCame;

namespace HaroohiePals.NitroKart.Extensions
{
    public static class MkdsMapDataKMPExport
    {
        private static bool skipUnknownObjs = true;

        private static float mapScale = 12.5f; // Default scale factor based on GLB export x 200 scale.
        private static float cameraFovScale = 3f;

        private static readonly Dictionary<MkdsCameraType, string> CameraTypesDict
            = new Dictionary<MkdsCameraType, string>
        {
            { MkdsCameraType.FixedLookAtDriver, "Fixed" },
            { MkdsCameraType.RouteLookAtDriver, "Path" },
            { MkdsCameraType.FixedLookAtTargets, "FixedMoveAt" },
            { MkdsCameraType.RouteLookAtTargets, "PathMoveAt" },
            { MkdsCameraType.IntroBottomPlaceholder, "PathMoveAt" },
            { MkdsCameraType.FollowDriverA, "Follow" },
            { MkdsCameraType.FollowDriverB, "Goal" },
            { MkdsCameraType.IntroMg, "FollowPath3" },
            { MkdsCameraType.MrFinish, "MissionSuccess" }
        };

        private static readonly Dictionary<MkdsAreaType, Byte> AreaTypesDict = new Dictionary<MkdsAreaType, Byte>
        {
            //{ MkdsAreaType.Type0, "" },
            { MkdsAreaType.Camera, 0 },
            //{ MkdsAreaType.MissionEnd, "" },
            { MkdsAreaType.ClipArea, 9 },
            { MkdsAreaType.EnemyRecalculation, 4 }
            //{ MkdsAreaType.MissionRivalPass, "" }
        };

        private static readonly Dictionary<MkdsMapObjectId, string> ObjectTypesDict
           = new Dictionary<MkdsMapObjectId, string>
        {
            //Ambient
            {MkdsMapObjectId.BeachWater, "Psea" },
            //{MkdsMapObjectId.FallsWaterDst, "itembox" },
            //{MkdsMapObjectId.TownWater, "itembox" },
            {MkdsMapObjectId.WaterSplash, "pocha" },
            //{MkdsMapObjectId.YoshiWater, "itembox" },
            {MkdsMapObjectId.AmbientSfxEmitter, "sound_audience" }, //depends on slot
            //{MkdsMapObjectId.HyudoroWater, "itembox" },
            //{MkdsMapObjectId.BirdSfxEmitter, "itembox" },
            //{MkdsMapObjectId.MissionBarrier, "itembox" },
            //{MkdsMapObjectId.MiniStage3Water, "itembox" },
            {MkdsMapObjectId.Puddle, "oilSFC" },
            {MkdsMapObjectId.LavaSplash, "pochaYogan" },
            {MkdsMapObjectId.HyudoroWaterSplash, "pocha" },
            //{MkdsMapObjectId.MrStageSteam, "itembox" },

            //Common
            {MkdsMapObjectId.Itembox, "itembox" },
            //{MkdsMapObjectId.DummyPole, "itembox" },
            {MkdsMapObjectId.Woodbox1, "woodbox" },
            {MkdsMapObjectId.Coin, "coin" },
            //{MkdsMapObjectId.BalloonController, "itembox" },
            //{MkdsMapObjectId.Shine, "itembox" },
            //{MkdsMapObjectId.ShineController, "itembox" },
            //{MkdsMapObjectId.Balloon, "itembox" },
            {MkdsMapObjectId.MissionGate, "starGate" },
            {MkdsMapObjectId.UnkItembox, "itembox" },
            //{MkdsMapObjectId.ShineBalloon, "itembox" },

            //Obstacle
            {MkdsMapObjectId.MoveItembox, "f_itembox" },
            {MkdsMapObjectId.KoopaBlock, "Crane" },
            //{MkdsMapObjectId.Gear, "itembox" },
            {MkdsMapObjectId.Bridge, "TownBridgeDSc" },
            //{MkdsMapObjectId.SecondHand, "itembox" },
            //{MkdsMapObjectId.TestCylinder, "itembox" },
            //{MkdsMapObjectId.Pendulum, "itembox" },
            //{MkdsMapObjectId.RotaryRoom, "itembox" },
            //{MkdsMapObjectId.RotaryBridge, "itembox" },
            //{MkdsMapObjectId.Dram, "itembox" },

            //Scenery
            {MkdsMapObjectId.BeachTree1Nocol, "peachtreeGC" },
            {MkdsMapObjectId.BeachTree1, "peachtreeGCc" },
            {MkdsMapObjectId.EarthenPipe, "dokan_sfc" },
            {MkdsMapObjectId.OpaTree1, "HeyhoTreeGBAc" },
            //{MkdsMapObjectId.OlgPipe1, "itembox" },
            //{MkdsMapObjectId.OlgMush1, "itembox" },
            //{MkdsMapObjectId.Of6Yoshi1, "itembox" },
            {MkdsMapObjectId.Cow, "cow" },
            //{MkdsMapObjectId.NsCannon1, "itembox" },
            //{MkdsMapObjectId.MiniDokan, "dokan_sfc" },
            {MkdsMapObjectId.GardenTree1, "gardentreeDSc" },
            {MkdsMapObjectId.CrossTree1, "gardentreeDS" },
            {MkdsMapObjectId.Teresa, "BGteresaSFC" },
            //{MkdsMapObjectId.Bakubaku, "itembox" },
            {MkdsMapObjectId.TeresaController, "b_teresa" },
            {MkdsMapObjectId.BankTree1, "DKtreeA64" },
            {MkdsMapObjectId.GardenTree1Nocol, "gardentreeDS" },
            //{MkdsMapObjectId.Chandelier, "itembox" },
            {MkdsMapObjectId.TownTree1Nocol, "TownTreeDS" },
            {MkdsMapObjectId.MarioTree3, "castletree1" },
            {MkdsMapObjectId.SnowTree1Nocol, "DKtreeA64" },
            {MkdsMapObjectId.TownTree1, "TownTreeDSc" },
            {MkdsMapObjectId.SnowTree1, "DKtreeA64c" },
            {MkdsMapObjectId.DeTree1Nocol, "DKtreeA64" },
            {MkdsMapObjectId.DeTree1, "DKtreeA64c" },
            //{MkdsMapObjectId.BankEgg1, "itembox" },
            //{MkdsMapObjectId.KinoHouse1, "itembox" },
            //{MkdsMapObjectId.KinoHouse2, "itembox" },
            //{MkdsMapObjectId.KinoMount1, "itembox" },
            //{MkdsMapObjectId.KinoMount2, "itembox" },
            {MkdsMapObjectId.OlaTree1C, "HeyhoTreeGBAc" },
            {MkdsMapObjectId.OsaTree1C, "HeyhoTreeGBAc" },
            //{MkdsMapObjectId.Picture1, "itembox" },
            //{MkdsMapObjectId.Picture2, "itembox" },
            {MkdsMapObjectId.Om6Tree1, "castletree1" },
            //{MkdsMapObjectId.RainStar, "itembox" },
            {MkdsMapObjectId.Of6TreeNocol, "castletree1" },
            {MkdsMapObjectId.Of6Tree, "castletree1c" },
            {MkdsMapObjectId.TownMonte, "monte_a" },
            //{MkdsMapObjectId.Airship, "itembox" },
            //{MkdsMapObjectId.StopSign, "itembox" },

            //Enemy
            {MkdsMapObjectId.Kuribo, "kuribo" },
            {MkdsMapObjectId.Rock, "DKrockGC" },
            {MkdsMapObjectId.Dossun, "dossunc" },
            //{MkdsMapObjectId.Stubbed404, "itembox" },
            {MkdsMapObjectId.Bus, "kart_truck" },
            {MkdsMapObjectId.WanwanFixed, "wanwan" },
            {MkdsMapObjectId.WanwanChain, "pile" },
            {MkdsMapObjectId.MkdEfBubble, "boble" },
            {MkdsMapObjectId.Choropu, "choropu2" },
            {MkdsMapObjectId.Car, "car_body" },
            {MkdsMapObjectId.Pukupuku, "pukupuku" },
            {MkdsMapObjectId.Truck, "kart_truck" },
            {MkdsMapObjectId.Snowman, "penguin_s" },
            //{MkdsMapObjectId.Kanoke64, "itembox" },
            {MkdsMapObjectId.Basabasa, "basabasa" },
            {MkdsMapObjectId.BasabasaSpawner, "basabasa" },
            //{MkdsMapObjectId.NsKiller1, "itembox" },
            //{MkdsMapObjectId.NsKiller2, "itembox" },
            //{MkdsMapObjectId.MoveTree, "itembox" },
            {MkdsMapObjectId.MkdEfBurner, "FlamePole_v" },
            {MkdsMapObjectId.WanwanMove, "Hwanwan" },
            {MkdsMapObjectId.ObPakkun, "puchi_pakkun" },
            {MkdsMapObjectId.Poo, "choropu2" }, // rocky wrench
            //{MkdsMapObjectId.Bound, "itembox" },
            //{MkdsMapObjectId.Flipper, "itembox" },
            {MkdsMapObjectId.Pakkun, "pakkun_f" },
            {MkdsMapObjectId.PakkunFire, "pakkun_f" },
            {MkdsMapObjectId.Crab, "crab" },
            {MkdsMapObjectId.Sun, "sunDS" },
            {MkdsMapObjectId.SunFireSnake, "FireSnake" },
            {MkdsMapObjectId.Fireball2, "WLfirebarGC" },
            {MkdsMapObjectId.IronBall, "Twanwan" },
            {MkdsMapObjectId.Rock2, "DKrockGC" },
            {MkdsMapObjectId.Sanbo, "sanbo" },
            {MkdsMapObjectId.IronBallNocol, "Twanwan" },
            //{MkdsMapObjectId.Cream, "itembox" },
            //{MkdsMapObjectId.Berry, "itembox" },
            //{MkdsMapObjectId.Unk438MrParticle, "itembox" },

            //Boss
            //{MkdsMapObjectId.BossDonketu, "itembox" },
            //{MkdsMapObjectId.KingIceDonketu, "itembox" },
            //{MkdsMapObjectId.KuriKing, "itembox" },
            //{MkdsMapObjectId.BombKing, "itembox" },
            //{MkdsMapObjectId.IwanteL, "itembox" },
            {MkdsMapObjectId.Hanachan, "hanachan" },
            //{MkdsMapObjectId.KingTeresa, "itembox" },
            //{MkdsMapObjectId.HanachanBodyPart, "itembox" },
            //{MkdsMapObjectId.Unk511MrParticle, "itembox" },
            //{MkdsMapObjectId.IwanteController, "itembox" },
        };

        public static byte[] WriteKMPJson(this MkdsMapData mapData)
        {
            var MapObjects = mapData.MapObjects;
            var Paths = mapData.Paths;
            var StageInfo = mapData.StageInfo;
            var StartPoints = mapData.StartPoints;
            var RespawnPoints = mapData.RespawnPoints;
            var KartPoint2D = mapData.KartPoint2D;
            var CannonPoints = mapData.CannonPoints;
            var KartPointMission = mapData.KartPointMission;
            var CheckPointPaths = mapData.CheckPointPaths;
            var ItemPaths = mapData.ItemPaths;
            var EnemyPaths = mapData.EnemyPaths;
            var MgEnemyPaths = mapData.MgEnemyPaths;
            var Areas = mapData.Areas;
            var Cameras = mapData.Cameras;

            //var serializer = CreateSerializer();
            string result;
            using (var stringWriter = new StringWriter())
            {
                using (var writer = new JsonTextWriter(stringWriter))
                {
                    writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                    writer.Indentation = 4;
                    writer.WriteStartObject();
                    {
                        writer.WritePropertyName("mRevision");
                        writer.WriteValue(2520);

                        writer.WritePropertyName("mOpeningPanIndex");
                        int OpeningPanIndex = 0;
                        // Get first camera marked as FirstIntro
                        for (int i = 0; i < Cameras.Count; i++)
                        {
                            if (Cameras[i].FirstIntroCamera == MkdsCameIntroCamera.Top)
                            {
                                OpeningPanIndex = i;
                                break;
                            }
                        }
                        writer.WriteValue(OpeningPanIndex);

                        writer.WritePropertyName("mVideoPanIndex");
                        writer.WriteValue(0); // Unused

                        writer.WritePropertyName("mStartPoints");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < StartPoints.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("_");
                                writer.WriteValue(i);

                                writer.WritePropertyName("player_index");
                                writer.WriteValue(StartPoints[i].Index);

                                writer.WritePropertyName("position");
                                SerializeVector3(writer, StartPoints[i].Position, mapScale);

                                writer.WritePropertyName("rotation");
                                SerializeVector3(writer, StartPoints[i].Rotation);

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mEnemyPaths");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < EnemyPaths.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("mPredecessors");
                                writer.WriteStartArray();
                                if (EnemyPaths[i].Previous.Count > 0)
                                {
                                    foreach (var path in EnemyPaths[i].Previous)
                                    {
                                        writer.WriteValue(EnemyPaths.IndexOf(path.Target));
                                    }
                                }
                                else
                                    writer.WriteValue(0);

                                writer.WriteEndArray();

                                writer.WritePropertyName("mSuccessors");
                                writer.WriteStartArray();
                                if (EnemyPaths[i].Next.Count > 0)
                                {
                                    foreach (var path in EnemyPaths[i].Next)
                                    {
                                        writer.WriteValue(EnemyPaths.IndexOf(path.Target));
                                    }
                                }
                                else
                                    writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("misc");
                                writer.WriteStartArray();
                                writer.WriteValue(0);
                                writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("points");
                                writer.WriteStartArray();

                                for (int j = 0; j < EnemyPaths[i].Points.Count; j++)
                                {
                                    writer.WriteStartObject();

                                    writer.WritePropertyName("deviation");
                                    writer.WriteValue(EnemyPaths[i].Points[j].Radius * 0.02 * mapScale);

                                    writer.WritePropertyName("param");
                                    writer.WriteStartArray();
                                    writer.WriteValue(0);
                                    writer.WriteValue(EnemyPaths[i].Points[j].Drifting.ToString() == "EndDrift" ? 1 : 0);
                                    writer.WriteValue(0);
                                    writer.WriteValue(0);
                                    writer.WriteEndArray();

                                    writer.WritePropertyName("position");
                                    SerializeVector3(writer, EnemyPaths[i].Points[j].Position, mapScale);

                                    writer.WriteEndObject();
                                }
                                writer.WriteEndArray();
                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mItemPaths");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < ItemPaths.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("mPredecessors");
                                writer.WriteStartArray();
                                if (ItemPaths[i].Previous.Count > 0)
                                {
                                    foreach (var path in ItemPaths[i].Previous)
                                    {
                                        writer.WriteValue(ItemPaths.IndexOf(path.Target));
                                    }
                                }
                                else
                                    writer.WriteValue(0);

                                writer.WriteEndArray();

                                writer.WritePropertyName("mSuccessors");
                                writer.WriteStartArray();
                                if (ItemPaths[i].Next.Count > 0)
                                {
                                    foreach (var path in ItemPaths[i].Next)
                                    {
                                        writer.WriteValue(ItemPaths.IndexOf(path.Target));
                                    }
                                }
                                else
                                    writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("misc");
                                writer.WriteStartArray();
                                writer.WriteValue(0);
                                writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("points");
                                writer.WriteStartArray();

                                for (int j = 0; j < ItemPaths[i].Points.Count; j++)
                                {
                                    writer.WriteStartObject();

                                    writer.WritePropertyName("deviation");
                                    writer.WriteValue(ItemPaths[i].Points[j].Radius * 0.02 * mapScale);

                                    writer.WritePropertyName("param");
                                    writer.WriteStartArray();
                                    writer.WriteValue(0);
                                    writer.WriteValue(0);
                                    writer.WriteValue(0);
                                    writer.WriteValue(0);
                                    writer.WriteEndArray();

                                    writer.WritePropertyName("position");
                                    SerializeVector3(writer, ItemPaths[i].Points[j].Position, mapScale);

                                    writer.WriteEndObject();
                                }
                                writer.WriteEndArray();
                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mCheckPaths");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < CheckPointPaths.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("mPredecessors");
                                writer.WriteStartArray();
                                if (CheckPointPaths[i].Previous.Count > 0)
                                {
                                    foreach (var path in CheckPointPaths[i].Previous)
                                    {
                                        writer.WriteValue(CheckPointPaths.IndexOf(path.Target));
                                    }
                                }
                                else
                                    writer.WriteValue(0);

                                writer.WriteEndArray();

                                writer.WritePropertyName("mSuccessors");
                                writer.WriteStartArray();
                                if (CheckPointPaths[i].Next.Count > 0)
                                {
                                    foreach (var path in CheckPointPaths[i].Next)
                                    {
                                        writer.WriteValue(CheckPointPaths.IndexOf(path.Target));
                                    }
                                }
                                else
                                    writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("misc");
                                writer.WriteStartArray();
                                writer.WriteValue(0);
                                writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("points");
                                writer.WriteStartArray();

                                for (int j = 0; j < CheckPointPaths[i].Points.Count; j++)
                                {
                                    writer.WriteStartObject();

                                    writer.WritePropertyName("mLapCheck");
                                    if (j == 0 && i == 0)
                                        writer.WriteValue(0);
                                    else
                                        writer.WriteValue(CheckPointPaths[i].Points[j].KeyPointId >= 0 ? CheckPointPaths[i].Points[j].KeyPointId + 1 : 255);

                                    writer.WritePropertyName("mLeft");
                                    SerializeVector2(writer, CheckPointPaths[i].Points[j].Point1, mapScale);

                                    writer.WritePropertyName("mRespawnIndex");
                                    writer.WriteValue(CheckPointPaths[i].Points[j].Respawn == null ? 255 : RespawnPoints.IndexOf(CheckPointPaths[i].Points[j].Respawn.Target));

                                    writer.WritePropertyName("mRight");
                                    SerializeVector2(writer, CheckPointPaths[i].Points[j].Point2, mapScale);

                                    writer.WriteEndObject();
                                }
                                writer.WriteEndArray();
                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mPaths");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < Paths.Count; i++)
                            {
                                MkdsCamera pathCamera = new MkdsCamera();
                                bool isCameraRoute = false;

                                foreach (var checkCamera in Cameras)
                                {
                                    if (checkCamera.Path != null)
                                    {
                                        if (checkCamera.Path.Target == Paths[i] && checkCamera.PathSpeed > 0)
                                        {
                                            pathCamera = checkCamera;
                                            isCameraRoute = true;
                                            break;
                                        }
                                    }
                                }

                                writer.WriteStartObject();

                                writer.WritePropertyName("interpolation");
                                writer.WriteValue(isCameraRoute && Paths[i].Points.Count >= 4 ? 1 : 0);

                                writer.WritePropertyName("loopPolicy");
                                writer.WriteValue(Convert.ToInt32(Paths[i].Loop));

                                writer.WritePropertyName("points");
                                writer.WriteStartArray();

                                for (int j = 0; j < Paths[i].Points.Count; j++)
                                {
                                    if (isCameraRoute && Paths[i].Points.Count >= 4)
                                    {
                                        //Skip first and last control points
                                        if (j <= 0 || j >= Paths[i].Points.Count - 1)
                                            continue;
                                    }

                                    writer.WriteStartObject();

                                    writer.WritePropertyName("params");
                                    writer.WriteStartArray();

                                    if (isCameraRoute)
                                    {
                                        if (Paths[i].Points.Count >= 4)
                                        {
                                            var routeTime = (int)((Paths[i].Points.Count - 3) / pathCamera.PathSpeed);
                                            writer.WriteValue((int)((GetTotalPathLength(Paths[i]) * mapScale) / routeTime));
                                        }
                                        else
                                        {
                                            var routeTime = (int)((Paths[i].Points.Count - 1) / pathCamera.PathSpeed);
                                            writer.WriteValue((int)((GetTotalPathLength(Paths[i], false) * mapScale) / routeTime));
                                        }
                                    }
                                    else
                                        writer.WriteValue(0);

                                    writer.WriteValue(0);
                                    writer.WriteEndArray();

                                    writer.WritePropertyName("position");
                                    SerializeVector3(writer, Paths[i].Points[j].Position, mapScale);

                                    writer.WriteEndObject();
                                }
                                writer.WriteEndArray();
                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mGeoObjs");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < MapObjects.Count; i++)
                            {
                                SerialiseObjTypeParams(MapObjects[i], out string objType, out int[] objSettings);

                                if (objType == "")
                                    continue; //skip objects that don't exist in mkw

                                writer.WriteStartObject();

                                writer.WritePropertyName("_");
                                writer.WriteValue(0);

                                writer.WritePropertyName("flags");
                                writer.WriteValue(63); // todo: idk

                                writer.WritePropertyName("id");
                                writer.WriteValue(objType); // todo: this

                                writer.WritePropertyName("pathId");
                                writer.WriteValue(MapObjects[i].Path == null ? -1 : Paths.IndexOf(MapObjects[i].Path.Target));

                                writer.WritePropertyName("position");
                                SerializeVector3(writer, MapObjects[i].Position, mapScale);

                                writer.WritePropertyName("rotation");
                                SerializeVector3(writer, MapObjects[i].Rotation);

                                writer.WritePropertyName("scale");
                                SerializeVector3(writer, MapObjects[i].Scale);

                                writer.WritePropertyName("settings"); // todo: this
                                writer.WriteStartArray();
                                writer.WriteValue(objSettings[0]);
                                writer.WriteValue(objSettings[1]);
                                writer.WriteValue(objSettings[2]);
                                writer.WriteValue(objSettings[3]);
                                writer.WriteValue(objSettings[4]);
                                writer.WriteValue(objSettings[5]);
                                writer.WriteValue(objSettings[6]);
                                writer.WriteValue(objSettings[7]);
                                writer.WriteEndArray();

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mAreas");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < Areas.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("mCameraIndex");
                                writer.WriteValue(Areas[i].Camera == null ? 255 : Cameras.IndexOf(Areas[i].Camera.Target));

                                writer.WritePropertyName("mEnemyLinkID");
                                if (Areas[i].EnemyPoint == null)
                                    writer.WriteValue(255);
                                else
                                    writer.WriteValue(GetPointID(mapData, Areas[i].EnemyPoint.Target));

                                writer.WritePropertyName("mModel");
                                writer.WriteStartObject();
                                writer.WritePropertyName("mPosition");
                                SerializeVector3(writer, Areas[i].Position, mapScale);
                                writer.WritePropertyName("mRotation");
                                SerializeVector3(writer, Areas[i].GetRotation());
                                writer.WritePropertyName("mScaling");
                                SerializeVector3(writer, new Vector3d(Areas[i].LengthVector.X * 0.01 * mapScale, Areas[i].LengthVector.Y * 0.01 * mapScale, Areas[i].LengthVector.Z * 0.01 * mapScale));
                                writer.WritePropertyName("mShape");
                                writer.WriteValue(Areas[i].Shape.ToString());
                                writer.WriteEndObject();

                                writer.WritePropertyName("mPad");
                                writer.WriteStartArray();
                                writer.WriteValue(0);
                                writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("mParameters");
                                writer.WriteStartArray();
                                writer.WriteValue(Areas[i].Param0);
                                writer.WriteValue(Areas[i].Param1);
                                writer.WriteEndArray();

                                writer.WritePropertyName("mPriority");
                                writer.WriteValue(0);

                                writer.WritePropertyName("mRailID");
                                writer.WriteValue(255);

                                writer.WritePropertyName("mType");
                                writer.WriteValue(SerializeAreaType(Areas[i].AreaType));

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mCameras");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < Cameras.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("mActiveFrames");
                                writer.WriteValue((float)Cameras[i].Duration);

                                writer.WritePropertyName("mFov");
                                writer.WriteStartObject();
                                writer.WritePropertyName("from");
                                writer.WriteValue((float)Cameras[i].FovBegin * cameraFovScale);
                                writer.WritePropertyName("mSpeed");
                                var FovDiff = Math.Abs((Cameras[i].FovBegin * cameraFovScale) - (Cameras[i].FovEnd * cameraFovScale));
                                writer.WriteValue((int)(Cameras[i].FovSpeed * FovDiff * 100f));
                                writer.WritePropertyName("to");
                                writer.WriteValue((float)Cameras[i].FovEnd * cameraFovScale);
                                writer.WriteEndObject();

                                writer.WritePropertyName("mMovieFlag");
                                writer.WriteValue(0);

                                writer.WritePropertyName("mNext");
                                writer.WriteValue(Cameras[i].NextCamera == null ? 255 : Cameras.IndexOf(Cameras[i].NextCamera.Target));

                                writer.WritePropertyName("mPathId");
                                writer.WriteValue(Cameras[i].Path == null ? 255 : Paths.IndexOf(Cameras[i].Path.Target));

                                writer.WritePropertyName("mPathSpeed");
                                if (Cameras[i].Path == null)
                                    writer.WriteValue(0);
                                else
                                {
                                    var routeTime = (int)((Cameras[i].Path.Target.Points.Count - 3) / Cameras[i].PathSpeed);
                                    writer.WriteValue((int)(GetTotalPathLength(Cameras[i].Path.Target) * mapScale / routeTime) * 100f); // todo
                                }

                                writer.WritePropertyName("mPosition");
                                SerializeVector3(writer, Cameras[i].Position, mapScale);

                                writer.WritePropertyName("mRotation");
                                SerializeVector3(writer, Cameras[i].Rotation);

                                writer.WritePropertyName("mShake");
                                writer.WriteValue(0);

                                writer.WritePropertyName("mStartFlag");
                                writer.WriteValue(0);

                                writer.WritePropertyName("mType");
                                writer.WriteValue(SerializeCameraType(Cameras[i].Type));

                                writer.WritePropertyName("mView");
                                writer.WriteStartObject();
                                writer.WritePropertyName("from");
                                if (Cameras[i].Type == MkdsCameraType.FixedLookAtTargets || Cameras[i].Type == MkdsCameraType.RouteLookAtTargets)
                                    SerializeVector3(writer, Cameras[i].Target1, mapScale);
                                else if (Cameras[i].Type == MkdsCameraType.FollowDriverA || Cameras[i].Type == MkdsCameraType.FollowDriverB)
                                    SerializeVector3(writer, new Vector3d(-Cameras[i].Target1.X, Cameras[i].Target1.Y, Cameras[i].Target1.Z));
                                else
                                    SerializeVector3(writer, Cameras[i].Target1);
                                writer.WritePropertyName("mSpeed");
                                var targetDistance = Vector3.Distance((Vector3)Cameras[i].Target1, (Vector3)Cameras[i].Target2);
                                writer.WriteValue((int)((targetDistance * mapScale) * Cameras[i].TargetSpeed * 100f));  // todo
                                writer.WritePropertyName("to");
                                if (Cameras[i].Type == MkdsCameraType.FixedLookAtTargets || Cameras[i].Type == MkdsCameraType.RouteLookAtTargets)
                                    SerializeVector3(writer, Cameras[i].Target2, mapScale);
                                else
                                    SerializeVector3(writer, new Vector3d(0,0,0), mapScale);
                                writer.WriteEndObject();

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mRespawnPoints");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < RespawnPoints.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("id");
                                writer.WriteValue(RespawnPoints[i].OriginalIndex);

                                writer.WritePropertyName("position");
                                SerializeVector3(writer, RespawnPoints[i].Position, mapScale);

                                writer.WritePropertyName("range");
                                writer.WriteValue(-1);

                                writer.WritePropertyName("rotation");
                                SerializeVector3(writer, RespawnPoints[i].Rotation);

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mCannonPoints");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < CannonPoints.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("mPosition");
                                SerializeVector3(writer, CannonPoints[i].Position, mapScale);

                                writer.WritePropertyName("mRotation");
                                SerializeVector3(writer, CannonPoints[i].Rotation);

                                writer.WritePropertyName("mType");
                                writer.WriteValue("Direct");

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mStages");
                        writer.WriteStartArray();
                        {
                            writer.WriteStartObject();

                            writer.WritePropertyName("_");
                            writer.WriteValue(0);

                            writer.WritePropertyName("mCorner");
                            writer.WriteValue(1); //placeholder

                            writer.WritePropertyName("mFlareTobi");
                            writer.WriteValue(1); //placeholder

                            writer.WritePropertyName("mLapCount");
                            writer.WriteValue(StageInfo.NrLaps);

                            writer.WritePropertyName("mLensFlareOptions"); //placeholder
                            writer.WriteStartObject();
                            writer.WritePropertyName("a");
                            writer.WriteValue(0);
                            writer.WritePropertyName("b");
                            writer.WriteValue(255);
                            writer.WritePropertyName("g");
                            writer.WriteValue(255);
                            writer.WritePropertyName("r");
                            writer.WriteValue(255);
                            writer.WriteEndObject();

                            writer.WritePropertyName("mSpeedModifier");
                            writer.WriteValue(0);

                            writer.WritePropertyName("mStartPosition");
                            writer.WriteValue(StageInfo.PolePosition);

                            writer.WritePropertyName("mUnk08");
                            writer.WriteValue(75); //placeholder (actually lensflare alpha?)

                            writer.WriteEndObject();
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mMissionPoints"); // todo
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < KartPointMission.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                    }
                    writer.WriteEndObject();
                }

                result = stringWriter.ToString();
            }

            return Encoding.ASCII.GetBytes(result);
        }

        public static void SerializeVector2(JsonWriter writer, Vector2d value, float scale = 1f)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.X * scale);
            writer.WriteValue(value.Y * scale);
            writer.WriteEndArray();
        }

        public static void SerializeVector3(JsonWriter writer, Vector3d value, float scale = 1f)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.X * scale);
            writer.WriteValue(value.Y * scale);
            writer.WriteValue(value.Z * scale);
            writer.WriteEndArray();
        }

        public static int GetPointID(MkdsMapData mapData, IMapDataEntry entry)
        {
            entry.GetPathPointIndices(mapData, out int pathIndex, out int pointIndex);

            int pointID = 0;

            switch (entry)
            {
                case MkdsPathPoint:
                    for (int i = 0; i < pathIndex; i++)
                    {
                        pointID += mapData.Paths[i].Points.Count;
                    }
                    pointID += pointIndex;
                    break;

                case MkdsItemPoint:
                    
                    for (int i = 0; i < pathIndex; i++)
                    {
                        pointID += mapData.ItemPaths[i].Points.Count;
                    }
                    pointID += pointIndex;
                    break;

                case MkdsEnemyPoint:

                    for (int i = 0; i < pathIndex; i++)
                    {
                        pointID += mapData.EnemyPaths[i].Points.Count;
                    }
                    pointID += pointIndex;
                    break;

                case MkdsMgEnemyPoint:

                    for (int i = 0; i < pathIndex; i++)
                    {
                        pointID += mapData.MgEnemyPaths[i].Points.Count;
                    }
                    pointID += pointIndex;
                    break;

                case MkdsCheckPoint:

                    for (int i = 0; i < pathIndex; i++)
                    {
                        pointID += mapData.CheckPointPaths[i].Points.Count;
                    }
                    pointID += pointIndex;
                    break;
            }

            return pointID;
        }

        public static string SerializeCameraType(MkdsCameraType type)
        {
            if (CameraTypesDict.ContainsKey(type))
                return CameraTypesDict[type];
            else
                return "Follow";
        }

        public static byte SerializeAreaType(MkdsAreaType type)
        {
            if (AreaTypesDict.ContainsKey(type))
                return AreaTypesDict[type];
            else
                return 0;
        }

        public static void SerialiseObjTypeParams(MkdsMapObject mapObj, out string objName, out int[] settings)
        {
            objName = "";
            settings = new int[8];
            if (ObjectTypesDict.ContainsKey(mapObj.ObjectId))
            {
                switch (mapObj.ObjectId)
                {
                    case MkdsMapObjectId.Fireball2:
                        if (mapObj.Settings.Settings[3] == 1)
                            objName = "WLfireringGC";
                        else
                            objName = "WLfirebarGC";
                        break;
                    default:
                        objName = ObjectTypesDict[mapObj.ObjectId];
                        break;
                }
            }
            else
            {
                if (!skipUnknownObjs)
                {
                    objName = mapObj.ObjectId.ToString();
                }
                else
                {
                    return;
                }
            }
        }

        public static double GetTotalPathLength(MkdsPath path, bool smooth = true)
        {

            if (path == null || path.Points.Count < 2)
                return 0f;

            double totalLength = 0f;

            if (path.Points.Count >= 4 && smooth)
            {
                for (int i = 1; i < path.Points.Count - 2; i++)
                {
                    totalLength += Vector3.Distance((Vector3)path.Points[i].Position, (Vector3)path.Points[i+1].Position);
                }
            }
            else
            {
                for (int i = 0; i < path.Points.Count - 1; i++)
                {
                    totalLength += Vector3.Distance((Vector3)path.Points[i].Position, (Vector3)path.Points[i + 1].Position);
                }
            }

            return totalLength;
        }
    }
}
