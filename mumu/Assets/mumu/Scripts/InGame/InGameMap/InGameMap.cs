using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// アイソメトリックのマップ。読み込み・経路の管理と、各クラス（表示・操作・演出）の組み立てを行う
    /// </summary>
    public class InGameMap : IMapCameraTarget
    {
        private GameObject mapRoot = null;
        private Block blockPrefab = null;
        private MapData mapData = null;
        private ThemeSprites sprites = null;
        private PugSprites pugSprites = null;
        private PuzzlePath path = null;

        private MapLayout layout = null;
        private TilePicker picker = null;
        private RefusalFeedback refusal = null;
        private MapBlockView blockView = null;
        private MapPathInput pathInput = null;
        private readonly MapMarkers markers = new MapMarkers();
        private readonly MoveSound moveSound = new MoveSound();
        private readonly MapClearSequence clearSequence = new MapClearSequence();
        private readonly MapDebugTools debugTools = new MapDebugTools();

        /// <summary>前回の反映時のこわれた橋の順番（新しく壊れたら知らせる）</summary>
        private int lastLockedIndex = -1;
        /// <summary>もどったことがあるか（ミッション判定用）</summary>
        private bool hasRewound = false;
        /// <summary>ヒント（出していなければ null）</summary>
        private HintPlan hint = null;
        /// <summary>このステージで橋が壊れたことを知らせたか</summary>
        private bool isCrumbleNotified = false;

        /// <summary>経路変更時の処理</summary>
        private Action onPathChangedAction = null;
        /// <summary>クリア時の処理</summary>
        private Action onClearedAction = null;
        /// <summary>画面下に出すお知らせの処理（ローカライズのキー）</summary>
        private Action<string> onMessageAction = null;

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

            if (!isValidMap())
            {
                return false;
            }

            sprites = await ThemeSprites.LoadAsync(mapData.themeId, _token);
            if (sprites == null)
            {
                Debug.LogError($"[InGameMap] Theme sprites not found: {ResourceManager.GetThemeSpritePath(mapData.themeId, "*")}");
                return false;
            }

            pugSprites = loadPugSprites(_goalCharacter, _playerCharacter);
            build();
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
            markers.SetArrowEnabled(false);
            refreshPath();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 設計した経路どおりに Goal まで進める（デバッグ用、クリア演出も通常どおり）
        /// </summary>
        public void DebugSolve()
        {
            debugTools.Solve(mapData, path, applyMove, refreshPath);
        }
#endif

        /// <summary>
        /// 各マスに経路の番号を表示（デバッグ用）
        /// </summary>
        /// <param name="_isVisible"></param>
        public void SetWaypointPreview(bool _isVisible)
        {
            debugTools.SetWaypointLabels(mapData, blockView, _isVisible);
        }

        /// <summary>
        /// 押した場所の種類（経路上なら巻き戻し、次に進めるマスなら進めてドラッグ開始）
        /// </summary>
        /// <param name="_screenPosition"></param>
        /// <param name="_camera"></param>
        public ePressTarget GetPressTarget(Vector2 _screenPosition, Camera _camera)
        {
            int countBefore = path.Cells.Count;
            ePressTarget target = pathInput.GetPressTarget(_screenPosition, _camera);
            // 押して経路が短くなった＝もどった（ドラッグの1マス戻しはここを通らないので数えない）
            if (path.Cells.Count < countBefore)
            {
                hasRewound = true;
            }
            return target;
        }

        /// <summary>
        /// 指を離してから経路をもどしたか（もどらずクリアのミッション）
        /// </summary>
        public bool HasRewound => hasRewound;


        /// <summary>
        /// ヒントを出す（設計した経路の一部。経路に乗ると普通の線になる）
        /// </summary>
        /// <param name="_seed"></param>
        public void ShowHint(int _seed)
        {
            hint = new HintPlan(HintPlan.GetDesignedPath(mapData), _seed);
            refreshPath();
        }

        /// <summary>
        /// 起点から半マス以上動いたか（動いていれば向き）
        /// </summary>
        /// <param name="_fromScreen"></param>
        /// <param name="_toScreen"></param>
        /// <param name="_camera"></param>
        /// <param name="_direction"></param>
        public bool TryGetDragDirection(Vector2 _fromScreen, Vector2 _toScreen, Camera _camera, out Vector2Int _direction)
        {
            return pathInput.TryGetDragDirection(_fromScreen, _toScreen, _camera, out _direction);
        }

        /// <summary>
        /// ドラッグで先端から向きへ1マス進める
        /// </summary>
        /// <param name="_direction"></param>
        public void DragStep(Vector2Int _direction)
        {
            pathInput.DragStep(_direction);
        }

        /// <summary>
        /// カメラを合わせる位置（経路の先端）
        /// </summary>
        public Vector2 GetFocusPosition()
        {
            return GetHeadPosition();
        }

        /// <summary>
        /// マップ全体の範囲（mapRoot の親の座標系）
        /// </summary>
        public Rect GetContentBounds()
        {
            Rect bounds = layout.ContentBounds();
            Vector2 offset = ((RectTransform)mapRoot.transform).anchoredPosition;
            return new Rect(bounds.position + offset, bounds.size);
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
        /// クリア演出（MapClearSequence に任せる）
        /// </summary>
        /// <param name="_token"></param>
        public UniTask PlayClearAsync(CancellationToken _token)
        {
            Action markGoalCleared = null;
            if (mapData.TryFindBlock(eBlockType.Goal, out Vector2Int goal))
            {
                markGoalCleared = () => getBlock(goal).SetCleared(true);
            }
            return clearSequence.PlayAsync(markers.PlayerPug, markGoalCleared, path.Cells, getBlock, _token);
        }

        /// <summary>
        /// マップデータの大きさと経路が正しいか
        /// </summary>
        private bool isValidMap()
        {
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
            return true;
        }

        /// <summary>
        /// パグの画像（主人公・Goal の子を指定のキャラクターに差し替える）
        /// </summary>
        /// <param name="_goalCharacter"></param>
        /// <param name="_playerCharacter"></param>
        private PugSprites loadPugSprites(CharacterSprites _goalCharacter, CharacterSprites _playerCharacter)
        {
            PugSprites result = PugSprites.Load();
            if (result != null && _goalCharacter != null)
            {
                result = result.WithGirl(_goalCharacter);
            }
            if (result != null && _playerCharacter != null)
            {
                result = result.WithBoy(_playerCharacter);
            }
            return result;
        }

        /// <summary>
        /// 経路と表示の部品を作り、ブロックとマーカーを並べて経路を反映する
        /// </summary>
        private void build()
        {
            path = new PuzzlePath(mapData);
            layout = new MapLayout(mapData);
            layout.CenterOffset = new Vector2((mapData.width - mapData.height) * MapLayout.HalfTileWidth * 0.5f, (mapData.width + mapData.height - 2) * MapLayout.HalfTileHeight * 0.5f);
            picker = new TilePicker(layout, mapData, (RectTransform)mapRoot.transform, getBlock);
            refusal = new RefusalFeedback(getBlock, msg => onMessageAction?.Invoke(msg));
            pathInput = new MapPathInput(picker, refusal, mapData, path, applyMove);

            blockView = new MapBlockView(layout);
            blockView.Create(blockPrefab, mapRoot.transform, mapData);
            blockView.ApplyMap(mapData, sprites, pugSprites, path.Checkpoints);
            markers.Create(mapRoot.transform, pugSprites, path, getBlock);
            refreshPath();
        }

        /// <summary>
        /// マスのブロック
        /// </summary>
        /// <param name="_cell"></param>
        private Block getBlock(Vector2Int _cell)
        {
            return blockView.Get(_cell);
        }

        /// <summary>
        /// マスの上面の中心（mapRoot の座標系）
        /// </summary>
        /// <param name="_cell"></param>
        private Vector2 getTopFacePosition(Vector2Int _cell)
        {
            return layout.TopFacePosition(_cell, getBlock(_cell).IsRaised);
        }

        /// <summary>
        /// 経路を表示に反映する（こわれた橋の知らせ・ブロック・マーカー・変更の通知）
        /// </summary>
        private void refreshPath()
        {
            // 初めて橋が壊れた時だけ知らせる
            int lockedIndex = path.LockedIndex;
            if (lockedIndex > lastLockedIndex && !isCrumbleNotified && markers.IsArrowEnabled)
            {
                isCrumbleNotified = true;
                onMessageAction?.Invoke("TOAST_CRUMBLE");
            }
            if (lockedIndex > lastLockedIndex)
            {
                CommonUtility.PlaySe(eSe.CrackWood);
            }
            lastLockedIndex = lockedIndex;

            blockView.ApplyPath(path, mapData, hint);
            markers.Refresh(path, getTopFacePosition);
            onPathChangedAction?.Invoke();
        }

        /// <summary>
        /// 持っているカギの数
        /// </summary>
        private int countKeys()
        {
            int count = 0;
            for (int color = 0; color < GimmickUtility.ColorCount; color++)
            {
                if (path.HasKey(color))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 進んだ時の鍵・扉・ワープの効果音
        /// </summary>
        /// <param name="_result"></param>
        /// <param name="_keysBefore"></param>
        private void playGimmickSound(eMoveResult _result, int _keysBefore)
        {
            if (_result != eMoveResult.Added)
            {
                return;
            }

            if (countKeys() > _keysBefore)
            {
                CommonUtility.PlaySe(eSe.KeyGet);
            }
            Vector2Int head = path.Head;
            foreach (eBlockSubType subType in mapData.GetBlockSubTypes(head.x, head.y))
            {
                if (GimmickUtility.IsGate(subType))
                {
                    CommonUtility.PlaySe(eSe.GateOpen);
                    break;
                }
            }
            if (path.IsWarpArrival(path.Cells.Count - 1))
            {
                CommonUtility.PlaySe(eSe.Warp);
            }
        }

        /// <summary>
        /// 指定マスへ進む・巻き戻して表示と効果音に反映
        /// </summary>
        /// <param name="_cell"></param>
        private eMoveResult applyMove(Vector2Int _cell)
        {
            bool wasGoalUnlocked = path.CanEnterGoal;
            int passedCheckpoints = path.PassedCheckpointCount;
            int keysBefore = countKeys();

            eMoveResult result = path.MoveTo(_cell);
            if (result == eMoveResult.None)
            {
                // こわれた橋より前を押した時も知らせる
                if (path.IsRewindLocked(_cell))
                {
                    refusal.CheckGimmick(_cell, path);
                }
                return result;
            }

            refusal.Reset();
            refreshPath();
            moveSound.Play(result, path.MoveCount, !wasGoalUnlocked && path.CanEnterGoal, path.PassedCheckpointCount > passedCheckpoints);
            playGimmickSound(result, keysBefore);

            if (result == eMoveResult.Cleared)
            {
                onClearedAction?.Invoke();
            }
            return result;
        }
    }
}
