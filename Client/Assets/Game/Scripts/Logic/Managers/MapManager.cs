using System;
using System.Collections.Generic;
using System.IO;
using Lockstep.Core;
using Lockstep.Math;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;

#endif
namespace Lockstep.Game {
    public interface IMapManager {
        TileInfos GetMapInfo(string name);
        void LoadLevel(int level);
        Vector2Int mapMin { get; }
        Vector2Int mapMax { get; }
    }

    [System.Serializable]
    public class MapManager : SingletonManager<MapManager>, IMapManager, IMapService {
        private static bool hasLoadIDMapConfig = false; // 是否已经加载了配置
        private static string idMapPath = "TileIDMap";
        private static TileBase[] id2Tiles = new TileBase[65536]; //64KB
        private static Dictionary<TileBase, ushort> tile2ID = new Dictionary<TileBase, ushort>();


        private static string TILE_MAP_NAME_BORN_POS = "BornPos";
        private static string TILE_MAP_NAME_GRASS = "Grass";

        public static string GetMapPathFull(int level){
            return Path.Combine(Application.dataPath, "Game/Resources/Maps/" + level + ".bytes");
        }


        public TileInfos GetMapInfo(string name){
            return gridInfo.GetMapInfo(name);
        }

        public GridInfo gridInfo { get; private set; }
        public Vector2Int mapMin { get; private set; }
        public Vector2Int mapMax { get; private set; }
        [SerializeField] public Grid grid { get; private set; }

        public List<LVector2> enemyBornPoints { get; private set; }
        public List<LVector2> playerBornPoss { get; private set; }
        public LVector2 campPos { get; private set; }

        public override void DoStart(){
            base.DoStart();
            if (grid == null) {
                grid = GameObject.FindObjectOfType<Grid>();
            }
        }

        public void LoadLevel(int level){
            gridInfo = MapManager.LoadMap(grid, level);
            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var tempInfo in gridInfo.tileMaps) {
                var tileMap = tempInfo.tilemap;
                var mapMin = tileMap.cellBounds.min;
                if (mapMin.x < min.x) min.x = mapMin.x;
                if (mapMin.y < min.y) min.y = mapMin.y;
                var mapMax = tileMap.cellBounds.max;
                if (mapMax.x > max.x) max.x = mapMax.x;
                if (mapMax.y > max.y) max.y = mapMax.y;
            }

            mapMin = min;
            mapMax = max;

            var tileInfo = GetMapInfo(Global.TileMapName_BornPos);
            var campPoss = tileInfo.GetAllTiles(MapManager.ID2Tile(Global.TileID_Camp));
            Debug.Assert(campPoss != null && campPoss.Count == 1, "campPoss!= null&& campPoss.Count == 1");
            campPos = campPoss[0];
            enemyBornPoints = tileInfo.GetAllTiles(MapManager.ID2Tile(Global.TileID_BornPosEnemy));
            playerBornPoss = tileInfo.GetAllTiles(MapManager.ID2Tile(Global.TileID_BornPosHero));

            if (_globalStateService != null) {
                _globalStateService.mapMin = mapMin;
                _globalStateService.mapMax = mapMax;
                _globalStateService.enemyBornPoints = enemyBornPoints;
                _globalStateService.playerBornPoss = playerBornPoss;
                _globalStateService.campPos = campPos;
            }


            Debug.Assert(_globalStateService.playerBornPoss.Count == Define.MAX_PLAYER_COUNT,
                "Map should has 2 player born pos");


            EventHelper.Trigger(EEvent.LoadMapDone, level);
        }

        void OnEvent_OnRoomGameStart(object param){
            LoadLevel(1);
        }


        public TileBase Pos2Tile(Vector2 pos, bool isCollider){
            return Pos2Tile(pos.Floor(), isCollider);
        }

        public ushort Pos2TileID(Vector2 pos, bool isCollider){
            return Pos2TileID(pos.Floor(), isCollider);
        }

        public void ReplaceTile(Vector2Int pos, ushort srcID, ushort targetID){
            foreach (var tilemap in gridInfo.tileMaps) {
                var tile = tilemap.GetTileID(pos);
                if (tile == srcID) {
                    tilemap.SetTileID(pos, targetID);
                }
            }
        }

        public TileBase Pos2TileIDPos2Tile(Vector2Int pos, bool isCollider){
            foreach (var tilemap in gridInfo.tileMaps) {
                if (isCollider && !tilemap.hasCollider) continue;
                var tile = tilemap.GetTile(pos);
                if (tile != null)
                    return tile;
            }

            return null;
        }

        public ushort Pos2TileID(Vector2Int pos, bool isCollider){
            for (int i = 0; i < gridInfo.tileMaps.Length; i++) {
                var tilemap = gridInfo.tileMaps[i];
                if (tilemap.isTagMap) continue;
                if (isCollider && !tilemap.hasCollider) continue;
                var tile = tilemap.GetTileID(pos);
                if (tile != 0)
                    return tile;
            }

            return 0;
        }

        public static GridInfo LoadMap(Grid grid, int level){
            CheckLoadTileIDMap();
            var path = MapManager.GetMapPathFull(level);
            if (!File.Exists(path)) {
                Debug.LogError("Have no map file" + level + " path:" + path);
                return null;
            }

            var bytes = File.ReadAllBytes(path);
            var reader = new BinaryReader(new MemoryStream(bytes));
            var info = TileMapSerializer.ReadGrid(reader);
            if (grid != null) {
                var maps = grid.GetComponentsInChildren<Tilemap>();
                for (int i = 0; i < maps.Length; i++) {
                    var tileMap = maps[i];
                    var tileMapInfo = info.GetMapInfo(tileMap.name);
                    if (tileMapInfo == null)
                        continue;
                    tileMapInfo.tilemap = tileMap;
                    tileMap.ClearAllTiles();
                    tileMap.SetTiles(tileMapInfo.GetAllPositions(), tileMapInfo.GetAllTiles());
                    if (Application.isPlaying) {
                        if (tileMap.name == TILE_MAP_NAME_BORN_POS) {
                            tileMap.GetComponent<TilemapRenderer>().enabled = false;
                        }
                    }

                    if (tileMap.name == TILE_MAP_NAME_BORN_POS
                        || tileMap.name == TILE_MAP_NAME_GRASS) {
                        tileMapInfo.hasCollider = false;
                    }

                    if (tileMap.name == TILE_MAP_NAME_BORN_POS) {
                        tileMapInfo.isTagMap = true;
                    }
                }
            }

            return info;
        }


        public static void SaveLevel(Grid grid, int level){
            if (grid == null) return;
            var bytes = TileMapSerializer.SerializeGrid(grid, MapManager.Tile2ID);
            if (bytes != null) {
                File.WriteAllBytes(MapManager.GetMapPathFull(level), bytes);
            }
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                AssetDatabase.Refresh();
            }

            Debug.LogFormat("SaveLevel {0} succ", level);
#endif
        }


        public static ushort Tile2ID(TileBase tile){
#if UNITY_EDITOR
            CheckLoadTileIDMap();
#endif
            if (tile == null)
                return 0;
            if (tile2ID.TryGetValue(tile, out ushort val)) {
                return val;
            }

            return 0;
        }

        public static TileBase ID2Tile(ushort tile){
#if UNITY_EDITOR
            CheckLoadTileIDMap();
#endif
            return id2Tiles[tile];
        }


        private static TileBase LoadTile(string relPath){
            var tile = Resources.Load<TileBase>(relPath);
            return tile;
        }

        public static void CheckLoadTileIDMap(){
            if (hasLoadIDMapConfig)
                return;
            hasLoadIDMapConfig = true;

            var file = Resources.Load<TextAsset>(idMapPath);
            if (file == null) {
                Debug.LogError("CheckLoadTileIDMap:LoadFileFailed " + idMapPath);
                return;
            }

            var txt = file.text;
            var allLines = txt.Replace("\r\n", "\n").Split('\n');
            var count = allLines.Length;
            tile2ID = new Dictionary<TileBase, ushort>(count);
            int i = 0;
            try {
                for (; i < count; i++) {
                    var str = allLines[i];
                    if (string.IsNullOrEmpty(str.Trim())) {
                        continue;
                    }

                    var strs = str.Split('=');
                    var id = ushort.Parse(strs[0].Trim());
                    var relPath = strs[1].Trim();
                    var tile = LoadTile(relPath);
                    id2Tiles[id] = tile;
                    tile2ID.Add(tile, id);
                }
            }
            catch (Exception e) {
                Debug.LogErrorFormat("CheckLoadTileIDMap:ParseError line = {0} str = {1} path = {2} e= {3}", i + 1,
                    allLines[i],
                    idMapPath, e.ToString());
                return;
            }
        }
    }
}