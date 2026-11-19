using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace mumu
{
    /// <summary>
    /// 押した場所の種類
    /// </summary>
    public enum ePressTarget
    {
        /// <summary>背景（マップ移動）</summary>
        Background,
        /// <summary>経路外のブロック（マップ移動）</summary>
        Block,
        /// <summary>経路の先端付近の経路外ブロック（誤操作防止のため何もしない）</summary>
        NearHead,
        /// <summary>経路上（経路ドラッグ）</summary>
        PathHead,
    }

    /// <summary>
    /// アイソメトリックのマップ表示と経路入力
    /// </summary>
    public class InGameMap
    {
        private static readonly Vector2Int NoCell = new Vector2Int(int.MinValue, int.MinValue);
        /// <summary>タッチ判定で調べるブロックの高さ</summary>
        private static readonly float[] CandidateHeights = { 0f, Block.RaiseHeight };

        /// <summary>経路の先端からこのマス数以内の経路外ブロックはマップ移動しない（2 = 5x5）</summary>
        private const int NoDragRange = 2;
        /// <summary>クリア時の跳ねる間隔（秒）</summary>
        private const float ClearWaveInterval = 0.06f;
        /// <summary>1手ごとの効果音ピッチの上がり幅</summary>
        private const float StepPitchRate = 0.04f;
        /// <summary>効果音ピッチの上限</summary>
        private const float MaxStepPitch = 2f;
        /// <summary>走るパグの大きさ（Goal のパグと同じ）</summary>
        private const float PlayerPugSize = 44f;
        /// <summary>矢印の大きさ</summary>
        private const float ArrowSize = 26f;
        /// <summary>上面の中心から矢印の先端までの高さ（座っているパグの頭の上）</summary>
        private const float ArrowOffsetY = 42f;
        /// <summary>クリア時、走るパグの到着を待つ最大時間（秒）</summary>
        private const float ArriveTimeout = 3f;
        /// <summary>経路の先端がこのマス数以内に来たら Goal の子が泣きやむ</summary>
        private const int GoalNoticeDistance = 3;

        private const int BlockSize = 60;
        private const int BlockSpacing = 0;
        /// <summary>ブロック中心からキューブ上面の中心までの高さ</summary>
        private const float TopFaceOffsetY = BlockSize * 0.25f;
        private const float TileWidth = BlockSize + BlockSpacing;
        private const float HalfTileWidth = TileWidth * 0.5f;
        private const float HalfTileHeight = TileWidth * 0.25f;

        private GameObject mapRoot = null;
        private Block blockPrefab = null;
        private MapData mapData = null;
        private Block[,] blockArray = null;
        private PuzzlePath path = null;
        private ThemeSprites sprites = null;
        private PugSprites pugSprites = null;
        private PlayerPug playerPug = null;
        private PointArrow pointArrow = null;
        /// <summary>矢印を表示するか（タイトルの見本では出さない）</summary>
        private bool isArrowEnabled = true;
        private readonly List<Vector2> pathPoints = new List<Vector2>();

        private float centerOffsetX = 0f;
        private float centerOffsetY = 0f;

        /// <summary>Goal に入れなかった演出を出したマス</summary>
        private Vector2Int refusedCell = NoCell;

        /// <summary>経路変更時の処理</summary>
        private Action onPathChangedAction = null;
        /// <summary>クリア時の処理</summary>
        private Action onClearedAction = null;
        /// <summary>画面下に出すお知らせの処理（ローカライズのキー）</summary>
        private Action<string> onMessageAction = null;
        /// <summary>前回の反映時のこわれた橋の順番（新しく壊れたら知らせる）</summary>
        private int lastLockedIndex = -1;
        /// <summary>このステージで橋が壊れたことを知らせたか</summary>
        private bool isCrumbleNotified = false;

        /// <summary>マップデータ</summary>
        public MapData MapData => mapData;
        /// <summary>テーマ画像</summary>
        public ThemeSprites Sprites => sprites;
        /// <summary>経路</summary>
        public PuzzlePath Path => path;
        /// <summary>パグの画像（読み込めなければ null）</summary>
        public PugSprites PugSprites => pugSprites;

        /// <summary>
        /// 読み込みとブロック生成
        /// </summary>
        /// <param name="_mapRoot"></param>
        /// <param name="_token"></param>
        /// <param name="_mapData"></param>
        /// <param name="_goalCharacter"></param>
        /// <param name="_playerCharacter"></param>
        public async UniTask<bool> InitAsync(GameObject _mapRoot, CancellationToken _token = default, MapData _mapData = null, CharacterSprites _goalCharacter = null, CharacterSprites _playerCharacter = null)
        {
            mapRoot = _mapRoot;

            // マップデータ未指定なら一覧の先頭のマップ（TODO: ステージ選択の追加後に差し替え）
            mapData = _mapData != null ? _mapData : MapCsv.LoadFirstMap();
            blockPrefab = await ResourceManager.Instance.LoadAsync<Block>(ResourceManager.GetInGamePrefabPath("block"), _token);
            if (blockPrefab == null || mapData == null)
            {
                return false;
            }

            if (!mapData.IsValidSize())
            {
                Debug.LogError($"[InGameMap] Invalid map data size: {mapData.width}x{mapData.height}");
                return false;
            }

            if (!mapData.ValidatePath(out string error))
            {
                Debug.LogError($"[InGameMap] Map path is invalid: {error}");
                return false;
            }

            sprites = await ThemeSprites.LoadAsync(mapData.themeId, _token);
            if (sprites == null)
            {
                Debug.LogError($"[InGameMap] Theme sprites not found: {ResourceManager.GetThemeSpritePath(mapData.themeId, "*")}");
                return false;
            }

            pugSprites = PugSprites.Load();
            if (pugSprites != null && _goalCharacter != null)
            {
                pugSprites = pugSprites.WithGirl(_goalCharacter);
            }
            if (pugSprites != null && _playerCharacter != null)
            {
                pugSprites = pugSprites.WithBoy(_playerCharacter);
            }
            path = new PuzzlePath(mapData);

            generateBlocks();
            applyMap();
            createMarkers();
            refreshPath();
            return true;
        }

        /// <summary>
        /// 経路変更時の処理を設定
        /// </summary>
        /// <param name="_action"></param>
        public void SetOnPathChanged(Action _action)
        {
            onPathChangedAction = _action;
        }

        /// <summary>
        /// 画面下のお知らせの処理を設定（引数はローカライズのキー）
        /// </summary>
        /// <param name="_action"></param>
        public void SetOnMessage(Action<string> _action)
        {
            onMessageAction = _action;
        }

        /// <summary>
        /// クリア時の処理を設定
        /// </summary>
        /// <param name="_action"></param>
        public void SetOnCleared(Action _action)
        {
            onClearedAction = _action;
        }

        /// <summary>
        /// 経路の一部を表示（タイトル画面の見本用）
        /// </summary>
        /// <param name="_rate"></param>
        public void ShowDemoPath(float _rate)
        {
            Vector2Int[] waypoints = mapData.GetWaypointPositions(out _);
            int count = Mathf.FloorToInt(waypoints.Length * Mathf.Clamp01(_rate));
            for (int i = 0; i < count; i++)
            {
                path.MoveTo(waypoints[i]);
            }
            isArrowEnabled = false;
            refreshPath();
        }

        /// <summary>
        /// 設計した経路どおりに Goal まで進める（デバッグ用、クリア演出も通常どおり）
        /// </summary>
        public void DebugSolve()
        {
            if (path.IsCleared || !mapData.TryFindBlock(eBlockType.Goal, out Vector2Int goal))
            {
                return;
            }

            path.Reset();
            foreach (Vector2Int waypoint in mapData.GetWaypointPositions(out _))
            {
                path.MoveTo(waypoint);
            }
            refreshPath();
            applyMove(goal);
        }

        /// <summary>
        /// 各マスに経路の番号を表示（デバッグ用）
        /// </summary>
        /// <param name="_isVisible"></param>
        public void SetWaypointPreview(bool _isVisible)
        {
            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    int waypoint = mapData.GetWaypointIndex(x, y);
                    blockArray[x, y].SetDebugLabel(_isVisible && waypoint >= 0 ? (waypoint + 1).ToString() : null);
                }
            }
        }

        /// <summary>
        /// 経路をリセット
        /// </summary>
        public void ResetPath()
        {
            path.Reset();
            refreshPath();
        }

        /// <summary>
        /// 押した場所の種類（経路上なら巻き戻し、次に進めるマスなら進めてドラッグ開始）
        /// </summary>
        /// <param name="_screenPosition"></param>
        /// <param name="_camera"></param>
        public ePressTarget GetPressTarget(Vector2 _screenPosition, Camera _camera)
        {
            // 水は背景扱い（tryGetCell の対象外）
            if (!toLocal(_screenPosition, _camera, out Vector2 local) || !tryGetCell(local, out Vector2Int cell))
            {
                return ePressTarget.Background;
            }

            refusedCell = NoCell;
            if (path.CanAdvanceTo(cell) || (path.CanBeginAt(cell) && cell != path.Head))
            {
                applyMove(cell);
                return ePressTarget.PathHead;
            }
            if (path.CanBeginAt(cell))
            {
                return ePressTarget.PathHead;
            }

            checkGoalRefused(cell);
            checkGimmickRefused(cell);
            return isNearHead(cell) ? ePressTarget.NearHead : ePressTarget.Block;
        }

        /// <summary>
        /// ドラッグ位置へ経路を伸ばす
        /// </summary>
        /// <param name="_screenPosition"></param>
        /// <param name="_camera"></param>
        public void DragPathTo(Vector2 _screenPosition, Camera _camera)
        {
            if (!toLocal(_screenPosition, _camera, out Vector2 local) || !tryGetCell(local, out Vector2Int cell))
            {
                return;
            }

            if (applyMove(cell) == eMoveResult.None)
            {
                checkGoalRefused(cell);
                checkGimmickRefused(cell);
            }
        }

        /// <summary>
        /// ギミック（やじるし・まがる・まっすぐ）で進めなかった演出（同じマスでは1回だけ）
        /// </summary>
        /// <param name="_cell"></param>
        private void checkGimmickRefused(Vector2Int _cell)
        {
            if (_cell == refusedCell)
            {
                return;
            }

            // こわれた橋より前へは戻れない
            if (path.IsRewindLocked(_cell))
            {
                refusedCell = _cell;
                playRefused(_cell);
                onMessageAction?.Invoke("TOAST_CRUMBLE_LOCKED");
                return;
            }

            bool isGate = path.IsBlockedByGate(_cell);
            if (!isGate && !path.IsBlockedByGimmick(_cell))
            {
                return;
            }

            refusedCell = _cell;
            if (isGate)
            {
                onMessageAction?.Invoke("TOAST_GATE");
            }
            playRefused(_cell);
        }

        /// <summary>
        /// 進めなかった演出（音・先端が揺れる・ギミックが跳ねる）
        /// </summary>
        /// <param name="_cell"></param>
        private void playRefused(Vector2Int _cell)
        {
            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.PlaySe(eSe.Refused);
            }
            getBlock(path.Head).PlayShake();
            getBlock(path.Head).PunchGimmick();
            getBlock(_cell).PunchGimmick();
        }

        /// <summary>
        /// 指定マスへ進む・巻き戻して表示と効果音に反映
        /// </summary>
        /// <param name="_cell"></param>
        private eMoveResult applyMove(Vector2Int _cell)
        {
            bool wasGoalUnlocked = path.CanEnterGoal;
            int passedCheckpoints = path.PassedCheckpointCount;

            eMoveResult result = path.MoveTo(_cell);
            if (result == eMoveResult.None)
            {
                // こわれた橋より前を押した時も知らせる
                if (path.IsRewindLocked(_cell))
                {
                    checkGimmickRefused(_cell);
                }
                return result;
            }

            refusedCell = NoCell;
            refreshPath();
            playMoveSe(result, !wasGoalUnlocked && path.CanEnterGoal, path.PassedCheckpointCount > passedCheckpoints);

            if (result == eMoveResult.Cleared)
            {
                onClearedAction?.Invoke();
            }
            return result;
        }

        /// <summary>
        /// マップ全体の範囲（mapRoot の親の座標系）
        /// </summary>
        public Rect GetContentBounds()
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            Vector2 half = new Vector2(BlockSize, BlockSize) * 0.5f;
            Vector2 raise = new Vector2(0f, Block.RaiseHeight);

            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    if (mapData.GetBlockType(x, y) == eBlockType.Empty)
                    {
                        continue;
                    }

                    Vector2 position = getBlockPosition(x, y);
                    min = Vector2.Min(min, position - half);
                    max = Vector2.Max(max, position + half + raise);
                }
            }

            Vector2 offset = ((RectTransform)mapRoot.transform).anchoredPosition;
            return Rect.MinMaxRect(min.x + offset.x, min.y + offset.y, max.x + offset.x, max.y + offset.y);
        }

        /// <summary>
        /// 経路の先端の上面の中心（mapRoot の親の座標系）
        /// </summary>
        public Vector2 GetHeadPosition()
        {
            return getTopFacePosition(path.Head) + ((RectTransform)mapRoot.transform).anchoredPosition;
        }

        /// <summary>
        /// Goal の上面の中心（mapRoot の親の座標系）
        /// </summary>
        public Vector2 GetGoalPosition()
        {
            mapData.TryFindBlock(eBlockType.Goal, out Vector2Int goal);
            return getTopFacePosition(goal) + ((RectTransform)mapRoot.transform).anchoredPosition;
        }

        /// <summary>
        /// クリア演出（走るパグが着いたら2匹が喜び、Start から順にブロックが跳ねる）
        /// </summary>
        /// <param name="_token"></param>
        public async UniTask PlayClearAsync(CancellationToken _token)
        {
            if (playerPug != null)
            {
                float waited = 0f;
                while (!playerPug.IsArrived && waited < ArriveTimeout)
                {
                    if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                    {
                        return;
                    }
                    waited += Time.unscaledDeltaTime;
                }
                playerPug.SetHappy(true);
            }

            if (mapData.TryFindBlock(eBlockType.Goal, out Vector2Int goal))
            {
                getBlock(goal).SetCleared(true);
            }

            IReadOnlyList<Vector2Int> cells = path.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                getBlock(cells[i]).PlayCelebrate();

                if (await UniTask.Delay(TimeSpan.FromSeconds(ClearWaveInterval), cancellationToken: _token).SuppressCancellationThrow())
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 移動の効果音
        /// </summary>
        /// <param name="_result"></param>
        /// <param name="_isGoalUnlocked"></param>
        /// <param name="_isCheckpointPassed"></param>
        private void playMoveSe(eMoveResult _result, bool _isGoalUnlocked, bool _isCheckpointPassed)
        {
            SoundManager sound = SoundManager.Instance;
            if (sound == null)
            {
                return;
            }

            // 手数が増えるほどピッチが上がる
            float pitch = Mathf.Min(MaxStepPitch, 1f + path.MoveCount * StepPitchRate);
            switch (_result)
            {
                case eMoveResult.Added:
                    sound.PlaySe(eSe.Step, pitch);
                    break;
                case eMoveResult.Undone:
                    sound.PlaySe(eSe.Undo, pitch);
                    break;
                case eMoveResult.Cleared:
                    sound.PlaySe(eSe.Clear);
                    return;
            }

            if (_isCheckpointPassed || _isGoalUnlocked)
            {
                sound.PlaySe(eSe.Checkpoint);
            }
        }

        /// <summary>
        /// 手数不足で Goal に入れなかった演出
        /// </summary>
        /// <param name="_cell"></param>
        private void checkGoalRefused(Vector2Int _cell)
        {
            bool isRefused = !path.IsCleared
                && mapData.GetBlockType(_cell.x, _cell.y) == eBlockType.Goal
                && MapData.IsAdjacent(path.Head, _cell)
                && !path.CanEnterGoal;

            if (!isRefused)
            {
                if (_cell != path.Head)
                {
                    refusedCell = NoCell;
                }
                return;
            }

            // 同じマスに乗っている間は1回だけ
            if (_cell == refusedCell)
            {
                return;
            }

            refusedCell = _cell;
            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.PlaySe(eSe.Refused);
            }
            getBlock(path.Head).PlayShake();
            getBlock(_cell).ShowMessage($"Need {path.RequiredMoveCount - path.MoveCount}");
        }

        /// <summary>
        /// スクリーン座標 → mapRoot のローカル座標
        /// </summary>
        /// <param name="_screenPosition"></param>
        /// <param name="_camera"></param>
        /// <param name="_local"></param>
        private bool toLocal(Vector2 _screenPosition, Camera _camera, out Vector2 _local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)mapRoot.transform, _screenPosition, _camera, out _local);
        }

        /// <summary>
        /// ローカル座標 → 見えているキューブ上面のマス
        /// </summary>
        /// <param name="_local"></param>
        /// <param name="_cell"></param>
        private bool tryGetCell(Vector2 _local, out Vector2Int _cell)
        {
            _cell = NoCell;
            int bestSortIndex = int.MinValue;

            // 低い位置・持ち上げ位置それぞれで推定したマスの周辺から、手前のブロックを優先
            foreach (float height in CandidateHeights)
            {
                Vector2Int estimate = estimateCell(_local, height);
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int x = estimate.x + dx;
                        int y = estimate.y + dy;
                        if (!mapData.IsInside(x, y))
                        {
                            continue;
                        }

                        Block block = blockArray[x, y];
                        if (!isPickable(block.BlockType) || block.SortIndex <= bestSortIndex || !isInTopFace(_local, x, y, block))
                        {
                            continue;
                        }

                        bestSortIndex = block.SortIndex;
                        _cell = new Vector2Int(x, y);
                    }
                }
            }

            return bestSortIndex != int.MinValue;
        }

        /// <summary>
        /// 経路の先端付近か（クリア後は対象外）
        /// </summary>
        /// <param name="_cell"></param>
        private bool isNearHead(Vector2Int _cell)
        {
            Vector2Int head = path.Head;
            return !path.IsCleared
                && Mathf.Abs(_cell.x - head.x) <= NoDragRange
                && Mathf.Abs(_cell.y - head.y) <= NoDragRange;
        }

        /// <summary>
        /// タッチ対象になるブロックか
        /// </summary>
        /// <param name="_type"></param>
        private static bool isPickable(eBlockType _type)
        {
            return _type != eBlockType.Empty && _type != eBlockType.Water;
        }

        /// <summary>
        /// ローカル座標からマスを推定（getBlockPosition の逆変換）
        /// </summary>
        /// <param name="_local"></param>
        /// <param name="_height"></param>
        private Vector2Int estimateCell(Vector2 _local, float _height)
        {
            float u = (_local.x + centerOffsetX) / HalfTileWidth;
            float v = (_local.y - TopFaceOffsetY - _height + centerOffsetY) / HalfTileHeight;
            int x = Mathf.RoundToInt((u + v) * 0.5f);
            int flippedY = Mathf.RoundToInt((v - u) * 0.5f);
            return new Vector2Int(x, (mapData.height - 1) - flippedY);
        }

        /// <summary>
        /// キューブ上面のひし形に含まれるか
        /// </summary>
        /// <param name="_local"></param>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_block"></param>
        private bool isInTopFace(Vector2 _local, int _x, int _y, Block _block)
        {
            Vector2 center = getBlockPosition(_x, _y);
            center.y += TopFaceOffsetY + (_block.IsRaised ? Block.RaiseHeight : 0f);

            Vector2 diff = _local - center;
            return Mathf.Abs(diff.x) / HalfTileWidth + Mathf.Abs(diff.y) / HalfTileHeight <= 1f;
        }

        /// <summary>
        /// マスの表示位置
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        private Vector2 getBlockPosition(int _x, int _y)
        {
            // エディタ上の上端（y=0）を画面奥に表示するため反転
            int flippedY = (mapData.height - 1) - _y;
            return new Vector2(
                (_x - flippedY) * HalfTileWidth - centerOffsetX,
                (_x + flippedY) * HalfTileHeight - centerOffsetY);
        }

        /// <summary>
        /// マスの上面の中心（mapRoot の座標系）
        /// </summary>
        /// <param name="_cell"></param>
        private Vector2 getTopFacePosition(Vector2Int _cell)
        {
            Vector2 position = getBlockPosition(_cell.x, _cell.y);
            position.y += TopFaceOffsetY + (getBlock(_cell).IsRaised ? Block.RaiseHeight : 0f);
            return position;
        }

        /// <summary>
        /// 走るパグと矢印を生成（ブロックより手前に表示）
        /// </summary>
        private void createMarkers()
        {
            if (pugSprites != null)
            {
                playerPug = PlayerPug.Create(mapRoot.transform, pugSprites, PlayerPugSize, new Vector2(HalfTileWidth, HalfTileHeight).magnitude);
                playerPug.SetHopCells(path.Checkpoints);
                playerPug.SetBlockProvider(getBlock);
                playerPug.SetWarpProvider(path.IsWarpArrival);
            }
            pointArrow = PointArrow.Create(mapRoot.transform, ArrowSize);
        }

        /// <summary>
        /// 走るパグと矢印を経路に合わせる
        /// </summary>
        private void refreshMarkers()
        {
            IReadOnlyList<Vector2Int> cells = path.Cells;
            pathPoints.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                pathPoints.Add(getTopFacePosition(cells[i]));
            }

            // クリア時は Goal のパグの隣に並ぶ
            if (path.IsCleared)
            {
                pathPoints[pathPoints.Count - 1] -= new Vector2(Block.GoalPairOffset, 0f);
            }

            if (playerPug != null)
            {
                playerPug.SetPath(cells, pathPoints);
            }

            if (pointArrow != null)
            {
                pointArrow.SetVisible(isArrowEnabled && !path.IsCleared);
                pointArrow.SetTarget(getTopFacePosition(path.Head) + new Vector2(0f, ArrowOffsetY));
            }
        }

        /// <summary>
        /// マスのブロック
        /// </summary>
        /// <param name="_cell"></param>
        private Block getBlock(Vector2Int _cell)
        {
            return blockArray[_cell.x, _cell.y];
        }

        /// <summary>
        /// ブロック生成
        /// </summary>
        private void generateBlocks()
        {
            int xCount = mapData.width;
            int yCount = mapData.height;

            blockArray = new Block[xCount, yCount];
            var blocks = new List<Block>(xCount * yCount);

            centerOffsetX = (xCount - yCount) * HalfTileWidth * 0.5f;
            centerOffsetY = (xCount + yCount - 2) * HalfTileHeight * 0.5f;

            var size = new Vector2(BlockSize, BlockSize);
            var center = new Vector2(0.5f, 0.5f);
            for (int x = 0; x < xCount; x++)
            {
                for (int y = 0; y < yCount; y++)
                {
                    Block newBlock = Object.Instantiate(blockPrefab, mapRoot.transform);
                    RectTransform rect = (RectTransform)newBlock.transform;

                    rect.anchorMin = center;
                    rect.anchorMax = center;
                    rect.pivot = center;
                    rect.localScale = Vector3.one;
                    rect.localRotation = Quaternion.identity;
                    rect.sizeDelta = size;
                    rect.anchoredPosition = getBlockPosition(x, y);

                    newBlock.SetOrder(x, (yCount - 1) - y, xCount, yCount);
                    blockArray[x, y] = newBlock;
                    blocks.Add(newBlock);
                }
            }

            // 奥から手前の順に並べる
            blocks.Sort((_a, _b) => _a.SortIndex.CompareTo(_b.SortIndex));
            for (int i = 0; i < blocks.Count; i++)
            {
                blocks[i].transform.SetSiblingIndex(i);
            }
        }

        /// <summary>
        /// マップデータをブロックに反映
        /// </summary>
        private void applyMap()
        {
            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    Block block = blockArray[x, y];
                    block.Setup(mapData.GetBlockType(x, y), mapData.GetBlockSubTypes(x, y), sprites);
                    block.SetupPug(pugSprites);
                    block.SetDecoVisible(!mapData.IsNearStartOrGoal(x, y));
                }
            }

            IReadOnlyList<Vector2Int> checkpoints = path.Checkpoints;
            for (int i = 0; i < checkpoints.Count; i++)
            {
                getBlock(checkpoints[i]).SetCheckpoint(i + 1);
            }
        }

        /// <summary>
        /// 経路をブロックに反映
        /// </summary>
        private void refreshPath()
        {
            IReadOnlyList<Vector2Int> cells = path.Cells;
            bool isCleared = path.IsCleared;
            bool canEnterGoal = path.CanEnterGoal;
            int last = cells.Count - 1;
            int lockedIndex = path.LockedIndex;

            // 初めて橋が壊れた時だけ知らせる
            if (lockedIndex > lastLockedIndex && !isCrumbleNotified && isArrowEnabled)
            {
                isCrumbleNotified = true;
                onMessageAction?.Invoke("TOAST_CRUMBLE");
            }
            lastLockedIndex = lockedIndex;

            // 経路に残るブロックのアニメーションを再生し直さないよう、1ブロック1回だけ反映する
            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    var cell = new Vector2Int(x, y);
                    Block block = blockArray[x, y];
                    int step = path.IndexOf(cell);

                    block.SetPathStep(step);
                    block.SetHighlight(path.CanAdvanceTo(cell));
                    block.SetGoalUnlocked(canEnterGoal);
                    block.SetGoalNoticed(!isCleared && Mathf.Abs(cell.x - path.Head.x) + Mathf.Abs(cell.y - path.Head.y) <= GoalNoticeDistance);
                    // クリア時の喜びは PlayClearAsync で走るパグの到着後に反映
                    if (!isCleared)
                    {
                        block.SetCleared(false);
                    }

                    // ワープで飛んだ所には線を引かない
                    Vector2Int toPrevious = step > 0 && !path.IsWarpArrival(step) ? cells[step - 1] - cell : Vector2Int.zero;
                    Vector2Int toNext = (step >= 0 && step < last && !path.IsWarpArrival(step + 1)) ? cells[step + 1] - cell : Vector2Int.zero;
                    block.SetLine(step >= 0, toPrevious, toNext);

                    eBlockSubType exclusive = GimmickUtility.GetExclusive(mapData.GetBlockSubTypes(x, y));
                    if (GimmickUtility.IsGate(exclusive))
                    {
                        block.SetGateOpen(path.HasKey(GimmickUtility.GetColorIndex(exclusive)));
                    }
                    block.SetCrumbled(exclusive == eBlockSubType.Crumble && step >= 0 && step <= lockedIndex);
                }
            }

            refreshMarkers();
            onPathChangedAction?.Invoke();
        }
    }
}
