using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ブロックの持ち上げ Animator を生成して block.prefab に設定
    /// 未設定ならコンパイル後に自動実行。既存のクリップ・Controller は上書きしない
    /// </summary>
    [InitializeOnLoad]
    public static class BlockAnimatorBuilder
    {
        private const string Folder = "Assets/mumu/Animation/Block";
        private const string ControllerPath = Folder + "/Block.controller";
        private const string PrefabPath = "Assets/mumu/Resources/Prefabs/InGame/block.prefab";
        private const string PropertyName = "m_AnchoredPosition.y";
        private const string StampKey = "BlockAnimatorBuilder";

        /// <summary>持ち上げ時に行き過ぎる量</summary>
        private const float Overshoot = 3f;
        private const float PopUpTime = 0.12f;
        private const float PopUpSettleTime = 0.22f;
        /// <summary>下げる時に行き過ぎる量</summary>
        private const float DropUndershoot = 2f;
        private const float DropTime = 0.1f;
        private const float DropSettleTime = 0.18f;

        /// <summary>
        /// コンパイル後に自動実行を予約
        /// </summary>
        static BlockAnimatorBuilder()
        {
            EditorApplication.delayCall += autoBuild;
        }

        /// <summary>
        /// 未設定なら生成
        /// </summary>
        private static void autoBuild()
        {
            bool hasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            if (EditorApplication.isPlayingOrWillChangePlaymode || !hasPrefab)
            {
                return;
            }
            if (!EditorBuildStamp.NeedsBuild(StampKey, isPrefabReady(), new[] { ControllerPath }))
            {
                return;
            }
            EditorBuildStamp.Save(StampKey);
            Build();
        }

        /// <summary>
        /// Animator を生成して設定
        /// </summary>
        [MenuItem("mumu/Build Block Animator")]
        public static void Build()
        {
            AnimatorController controller = loadOrCreateController();
            setupPrefab(controller);
            AssetDatabase.SaveAssets();
            EditorBuildStamp.Save(StampKey);
            Debug.Log($"[BlockAnimatorBuilder] Animator set to {PrefabPath}");
        }

        /// <summary>
        /// プレハブが設定済みか
        /// </summary>
        private static bool isPrefabReady()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                return true;
            }

            Animator animator = prefab.GetComponent<Animator>();
            return animator != null && animator.runtimeAnimatorController != null && prefab.transform.Find(Block.BodyName) != null;
        }

        /// <summary>
        /// Controller を読み込む（なければ生成）
        /// </summary>
        private static AnimatorController loadOrCreateController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller != null)
            {
                return controller;
            }

            EditorAssetUtility.EnsureFolder(Folder);

            float up = MapLayout.RaiseHeight;
            AnimationClip down = loadOrCreateClip(Block.StateDown, new Keyframe(0f, 0f));
            AnimationClip raised = loadOrCreateClip(Block.StateUp, new Keyframe(0f, up));
            AnimationClip popUp = loadOrCreateClip(Block.StatePopUp,
                new Keyframe(0f, 0f), new Keyframe(PopUpTime, up + Overshoot), new Keyframe(PopUpSettleTime, up));
            AnimationClip drop = loadOrCreateClip(Block.StateDrop,
                new Keyframe(0f, up), new Keyframe(DropTime, -DropUndershoot), new Keyframe(DropSettleTime, 0f));

            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            AnimatorState downState = addState(stateMachine, Block.StateDown, down, new Vector3(300f, 0f));
            addState(stateMachine, Block.StateUp, raised, new Vector3(300f, 80f));
            addState(stateMachine, Block.StatePopUp, popUp, new Vector3(550f, 0f));
            addState(stateMachine, Block.StateDrop, drop, new Vector3(550f, 80f));
            stateMachine.defaultState = downState;

            return controller;
        }

        /// <summary>
        /// ステートを追加
        /// </summary>
        /// <param name="_stateMachine"></param>
        /// <param name="_name"></param>
        /// <param name="_motion"></param>
        /// <param name="_position"></param>
        private static AnimatorState addState(AnimatorStateMachine _stateMachine, string _name, Motion _motion, Vector3 _position)
        {
            AnimatorState state = _stateMachine.AddState(_name, _position);
            state.motion = _motion;
            return state;
        }

        /// <summary>
        /// クリップを読み込む（なければ生成）
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_keys"></param>
        private static AnimationClip loadOrCreateClip(string _name, params Keyframe[] _keys)
        {
            string path = $"{Folder}/Block_{_name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
            {
                return clip;
            }

            // 1キーだと長さ0になるため固定値は2キーにする
            if (_keys.Length == 1)
            {
                _keys = new[] { _keys[0], new Keyframe(1f / 60f, _keys[0].value) };
            }

            clip = new AnimationClip { name = $"Block_{_name}" };
            var binding = EditorCurveBinding.FloatCurve(Block.BodyName, typeof(RectTransform), PropertyName);
            AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(_keys));
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        /// <summary>
        /// block > body > (既存の子) に組み替えて Animator を設定
        /// </summary>
        /// <param name="_controller"></param>
        private static void setupPrefab(RuntimeAnimatorController _controller)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.transform.Find(Block.BodyName) == null)
                {
                    var body = new GameObject(Block.BodyName, typeof(RectTransform));
                    body.layer = root.layer;

                    var bodyRect = (RectTransform)body.transform;
                    bodyRect.SetParent(root.transform, false);
                    bodyRect.anchorMin = Vector2.zero;
                    bodyRect.anchorMax = Vector2.one;
                    bodyRect.pivot = new Vector2(0.5f, 0.5f);
                    bodyRect.offsetMin = Vector2.zero;
                    bodyRect.offsetMax = Vector2.zero;

                    var children = new List<Transform>();
                    foreach (Transform child in root.transform)
                    {
                        if (child != bodyRect)
                        {
                            children.Add(child);
                        }
                    }
                    foreach (Transform child in children)
                    {
                        child.SetParent(bodyRect, false);
                    }
                }

                Animator animator = root.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = root.AddComponent<Animator>();
                }
                animator.runtimeAnimatorController = _controller;

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
