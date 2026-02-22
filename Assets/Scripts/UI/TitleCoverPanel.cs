using UnityEngine;
using UnityEngine.EventSystems;

namespace StrangePlaces.UI
{
    public class TitleCoverPanel : MonoBehaviour, IPointerClickHandler
    {
        // 静态变量会在整个游戏运行期间保留状态
        // 这样只要游戏不关，回到选关界面时它就是 true
        private static bool _hasShown = false;

        private void Start()
        {
            if (_hasShown)
            {
                // 如果已经显示过了，直接销毁自己，不让玩家看到
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // 如果还没按过，允许玩家按下任意键（键盘或鼠标）来关闭封面
            if (!_hasShown && Input.anyKeyDown)
            {
                Dismiss();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_hasShown)
            {
                Dismiss();
            }
        }

        private void Dismiss()
        {
            _hasShown = true;
            // 记录已显示后，销毁封面层
            Destroy(gameObject);
        }
    }
}
