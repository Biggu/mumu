using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// クリアの結果の札（ほね・★・もどったか）を出して、少し待ってから消す（Prefabs/Common/clear_summary）
    /// </summary>
    public static class ClearSummary
    {
        private const string PrefabPath = "Prefabs/Common/clear_summary";
        /// <summary>札を出している時間（秒）</summary>
        private const float ShowSeconds = 2.5f;

        /// <summary>
        /// 札を出して、時間がたったら消す（途中で止められたら true）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_bones"></param>
        /// <param name="_stars"></param>
        /// <param name="_isNoRewind"></param>
        /// <param name="_token"></param>
        public static async UniTask<bool> ShowAsync(Transform _parent, int _bones, int _stars, bool _isNoRewind, CancellationToken _token)
        {
            GameObject go = UnityEngine.Object.Instantiate(ResourceManager.Instance.Load<GameObject>(PrefabPath), _parent, false);
            go.transform.Find("bones").GetComponent<TMP_Text>().text = MumuText.SUMMARY_BONES(_bones);
            go.transform.Find("stars").GetComponent<TMP_Text>().text = MumuText.SUMMARY_STARS(_stars);
            go.transform.Find("norewind").GetComponent<TMP_Text>().text = (_isNoRewind ? MumuText.SUMMARY_NOREWIND() : MumuText.SUMMARY_REWOUND());

            // 札を押すか、時間がたつと次へ進む
            var tapped = new UniTaskCompletionSource();
            go.GetComponent<MumuButton>().SetOnClick(() => tapped.TrySetResult());
            await UniTask.WhenAny(UniTask.Delay(TimeSpan.FromSeconds(ShowSeconds), cancellationToken: _token), tapped.Task).SuppressCancellationThrow();
            UnityEngine.Object.Destroy(go);
            return _token.IsCancellationRequested;
        }
    }
}
