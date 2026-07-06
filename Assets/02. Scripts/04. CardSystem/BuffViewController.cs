using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Arcanist
{
    public class BuffViewController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Transform buffViewRoot;
        [SerializeField] private BuffUIController buffUIPrefab;

        private ComponentPool<BuffUIController> buffPool;
        private readonly Dictionary<string, BuffUIController> activeBuffUIs = new Dictionary<string, BuffUIController>();

        private void Awake()
        {
            Transform parent = buffViewRoot != null ? buffViewRoot : transform;
            buffPool = new ComponentPool<BuffUIController>(buffUIPrefab, parent);
        }

        public void ShowBuff(string buffId, Sprite icon, float duration)
        {
            if (string.IsNullOrEmpty(buffId))
            {
                Debug.LogWarning("BuffViewController : Buff Id가 비어 있습니다.");
                return;
            }

            if (buffUIPrefab == null)
            {
                Debug.LogWarning("BuffViewController : Buff UI Prefab이 연결되지 않았습니다.");
                return;
            }

            if (activeBuffUIs.TryGetValue(buffId, out BuffUIController buffUI))
            {
                buffUI.Initialize(icon, duration);
                return;
            }

            Transform parent = buffViewRoot != null ? buffViewRoot : transform;
            BuffUIController newBuffUI = buffPool.Get();
            newBuffUI.transform.SetParent(parent, false);
            newBuffUI.Initialize(icon, duration);
            activeBuffUIs.Add(buffId, newBuffUI);
        }

        public void HideBuff(string buffId)
        {
            if (string.IsNullOrEmpty(buffId))
                return;

            if (activeBuffUIs.TryGetValue(buffId, out BuffUIController buffUI) == false)
                return;

            activeBuffUIs.Remove(buffId);
            buffUI.Hide();
        }

        public void Clear()
        {
            foreach (BuffUIController buffUI in activeBuffUIs.Values)
                if (buffUI != null) buffUI.Hide();

            activeBuffUIs.Clear();
        }
    }
}
