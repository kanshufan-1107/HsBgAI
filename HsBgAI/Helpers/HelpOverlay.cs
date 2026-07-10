using UnityEngine;

namespace HsBgAI.Helpers
{
    /// <summary>
    /// F10 快捷键帮助信息的 GUI 覆盖层绘制
    /// </summary>
    public class HelpOverlay
    {
        private Texture2D _cachedBgTex;
        private GUIStyle _cachedHelpStyle;

        public bool IsVisible { get; set; }
        public string Message { get; set; } = "";
        private float _startTime;

        public void Show(string message)
        {
            Message = message;
            IsVisible = true;
            _startTime = Time.unscaledTime;
        }

        public void Update()
        {
            if (IsVisible && Time.unscaledTime - _startTime > 8f)
                IsVisible = false;
        }

        public void OnGUI()
        {
            if (!IsVisible) return;

            if (_cachedBgTex == null)
                _cachedBgTex = MakeTex(2, 2, new Color(0.8f, 0.8f, 0.8f, 0.7f));

            if (_cachedHelpStyle == null)
            {
                _cachedHelpStyle = new GUIStyle("box")
                {
                    fontSize = 16,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true,
                    padding = new RectOffset(10, 10, 10, 10),
                    normal = { textColor = Color.black, background = _cachedBgTex }
                };
            }

            float width = 300f;
            float padding = 20f;
            float x = Screen.width - width - padding;
            float y = padding;

            string[] lines = Message.Split('\n');
            float lineHeight = 20f;
            float titleHeight = 24f;
            float separatorHeight = 16f;

            float height = 0f;
            foreach (string line in lines)
            {
                if (line.Contains("快捷键帮助")) height += titleHeight;
                else if (line.Contains("──")) height += separatorHeight;
                else height += lineHeight;
            }
            height += 20f;

            GUI.Box(new Rect(x - 5, y - 5, width + 10, height + 10), "");
            GUI.Label(new Rect(x, y, width, height), Message, _cachedHelpStyle);

            float remainingTime = 8f - (Time.unscaledTime - _startTime);
            GUI.Label(new Rect(x, y + height + 5, width, 20),
                "自动隐藏: " + remainingTime.ToString("F1") + "秒",
                new GUIStyle("label")
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = Color.yellow }
                });
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
