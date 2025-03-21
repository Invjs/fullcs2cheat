﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CS2ExternalESP
{
    public static class Renderer
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool Rectangle(IntPtr hdc, int left, int top, int right, int bottom);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreatePen(int fnPenStyle, int nWidth, int crColor);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(int crColor);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("gdi32.dll")]
        private static extern int SetBkMode(IntPtr hdc, int mode);

        [DllImport("gdi32.dll")]
        private static extern int SetTextColor(IntPtr hdc, int color);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool TextOutW(IntPtr hdc, int x, int y, string lpString, int nCount);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static IntPtr desktopDC;
        private static bool menuVisible = false;
        private static DateTime lastToggleTime = DateTime.MinValue;

        
        public static bool ShowESPBoxes = true;
        public static bool ShowHealthBars = true;
        public static bool ShowBoneESP = true;
        public static float BoneThickness = 1.0f;
        private static bool showBoneThicknessSlider = false;
        private static bool isDraggingSlider = false;

        private static DateTime lastClickTime = DateTime.MinValue;
        private static Point lastMousePosition;

        
        private static List<Snowflake> snowflakes = new List<Snowflake>();
        private static Random random = new Random();
        private static DateTime lastSnowUpdate = DateTime.Now;

        
        private enum MenuTab { Visuals, Aimbot, Config, Colors }
        private static MenuTab currentTab = MenuTab.Visuals;

        
        private static Point menuPosition = new Point(50, 50);
        private static bool isDragging = false;
        private static Point dragOffset;

        
        public static Color EnemyColor = Color.Red;
        public static Color FriendlyColor = Color.Blue;
        public static Color FriendlyLineColor = Color.Blue;
        private static bool showEnemyColorPicker = false;
        private static bool showFriendlyColorPicker = false;

        
        public static Color EnemyLineColor = Color.Red;
        private static bool showEnemyLineColorPicker = false;
        private static bool showFriendlyLineColorPicker = false;

        
        private static readonly Font font = new Font("Segoe UI", 12);

        private class Snowflake
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Speed { get; set; }
            public float Size { get; set; }
        }

        static Renderer()
        {
            desktopDC = GetDC(GetDesktopWindow());
            if (desktopDC == IntPtr.Zero)
            {
                Console.WriteLine("Failed to get DC handle!");
            }

            
            for (int i = 0; i < 100; i++)
            {
                snowflakes.Add(new Snowflake
                {
                    X = random.Next(0, 1920),
                    Y = random.Next(-500, 0),
                    Speed = random.Next(1, 5),
                    Size = random.Next(2, 6)
                });
            }
        }

        public static void Update()
        {
            
            if ((GetAsyncKeyState(0x74) & 0x8000) != 0 && 
                (DateTime.Now - lastToggleTime).TotalMilliseconds > 200)
            {
                menuVisible = !menuVisible;
                lastToggleTime = DateTime.Now;
            }

            
            if ((GetAsyncKeyState(0x01) & 0x8000) != 0 && 
                (DateTime.Now - lastClickTime).TotalMilliseconds > 200)
            {
                HandleMenuClick();
                lastClickTime = DateTime.Now;
            }

            
            lastMousePosition = Cursor.Position;

            
            if ((DateTime.Now - lastSnowUpdate).TotalMilliseconds > 16)
            {
                UpdateSnow();
                lastSnowUpdate = DateTime.Now;
            }

            
            if ((GetAsyncKeyState(0x01) & 0x8000) != 0) 
            {
                Point mousePos = Control.MousePosition;

                if (!isDragging)
                {
                    
                    if (mousePos.X >= menuPosition.X && mousePos.X <= menuPosition.X + 300 &&
                        mousePos.Y >= menuPosition.Y && mousePos.Y <= menuPosition.Y + 40)
                    {
                        isDragging = true;
                        dragOffset = new Point(mousePos.X - menuPosition.X, mousePos.Y - menuPosition.Y);
                    }
                }
                else
                {
                    // Move menu with mouse
                    menuPosition = new Point(mousePos.X - dragOffset.X, mousePos.Y - dragOffset.Y);
                }
            }
            else
            {
                isDragging = false;
            }
        }

        private static void HandleMenuClick()
        {
            if (!menuVisible) return;

            const int menuWidth = 400;
            const int menuHeight = 500;

            Point mousePos = Control.MousePosition;

            
            int relativeX = mousePos.X - menuPosition.X;
            int relativeY = mousePos.Y - menuPosition.Y;

            
            if (relativeX >= 0 && relativeX <= menuWidth &&
                relativeY >= 0 && relativeY <= menuHeight)
            {
                // Check tab clicks
                if (relativeY >= 60 && relativeY <= 80)
                {
                    if (relativeX >= 20 && relativeX <= 80)
                    {
                        currentTab = MenuTab.Visuals;
                    }
                    else if (relativeX >= 120 && relativeX <= 180)
                    {
                        currentTab = MenuTab.Aimbot;
                    }
                    else if (relativeX >= 220 && relativeX <= 280)
                    {
                        currentTab = MenuTab.Config;
                    }
                    else if (relativeX >= 320 && relativeX <= 380)
                    {
                        currentTab = MenuTab.Colors;
                    }
                }

                
                switch (currentTab)
                {
                    case MenuTab.Visuals:
                        HandleVisualsClick(relativeX, relativeY);
                        break;
                    case MenuTab.Aimbot:
                        HandleAimbotClick(relativeX, relativeY);
                        break;
                    case MenuTab.Config:
                        HandleConfigClick(relativeX, relativeY);
                        break;
                    case MenuTab.Colors:
                        HandleColorsClick(relativeX, relativeY);
                        break;
                }
            }
        }

        private static void HandleVisualsClick(int relativeX, int relativeY)
        {
            const int itemHeight = 40;

            if (relativeX >= 20 && relativeX <= 280)
            {
                if (relativeY >= 100 && relativeY <= 100 + itemHeight)
                {
                    ShowESPBoxes = !ShowESPBoxes;
                }
                else if (relativeY >= 140 && relativeY <= 140 + itemHeight)
                {
                    ShowHealthBars = !ShowHealthBars;
                }
                else if (relativeY >= 180 && relativeY <= 180 + itemHeight)
                {
                    ShowBoneESP = !ShowBoneESP;
                    showBoneThicknessSlider = ShowBoneESP;
                }
            }

            
            if (showBoneThicknessSlider)
            {
                HandleSliderInteraction(relativeX, relativeY, 20, 220, ref BoneThickness, 0.5f, 3.0f);
            }
        }

        private static void HandleAimbotClick(int relativeX, int relativeY)
        {
            
        }

        private static void HandleConfigClick(int relativeX, int relativeY)
        {
            
        }

        private static void HandleColorsClick(int relativeX, int relativeY)
        {
            
            if (relativeX >= 20 && relativeX <= 170 && relativeY >= 100 && relativeY <= 120)
            {
                showEnemyColorPicker = !showEnemyColorPicker;
                showFriendlyColorPicker = false;
                showFriendlyLineColorPicker = false;
            }

            
            if (relativeX >= 20 && relativeX <= 170 && relativeY >= 150 && relativeY <= 170)
            {
                showFriendlyColorPicker = !showFriendlyColorPicker;
                showEnemyColorPicker = false;
                showFriendlyLineColorPicker = false;
            }

            
            if (relativeX >= 20 && relativeX <= 170 && relativeY >= 200 && relativeY <= 220)
            {
                showFriendlyLineColorPicker = !showFriendlyLineColorPicker;
                showEnemyColorPicker = false;
                showFriendlyColorPicker = false;
            }

            
            if (showEnemyColorPicker)
            {
                HandleColorWheelClick(relativeX, relativeY, ref EnemyColor);
            }
            else if (showFriendlyColorPicker)
            {
                HandleColorWheelClick(relativeX, relativeY, ref FriendlyColor);
            }
            else if (showFriendlyLineColorPicker)
            {
                HandleColorWheelClick(relativeX, relativeY, ref FriendlyLineColor);
            }
        }

        public static void DrawMenu(Graphics g)
        {
            if (!menuVisible) return;

            
            const int menuWidth = 400;
            const int menuHeight = 500;

            // Draw snow
            using (var snowBrush = new SolidBrush(Color.White))
            {
                foreach (var flake in snowflakes)
                {
                    if (flake.X >= menuPosition.X && flake.X <= menuPosition.X + menuWidth &&
                        flake.Y >= menuPosition.Y && flake.Y <= menuPosition.Y + menuHeight)
                    {
                        g.FillEllipse(snowBrush, flake.X, flake.Y, flake.Size, flake.Size);
                    }
                }
            }

            
            using (var backgroundBrush = new SolidBrush(Color.FromArgb(220, 30, 30, 30)))
            using (var path = GetRoundedRectanglePath(menuPosition.X, menuPosition.Y, menuWidth, menuHeight, 10))
            {
                g.FillPath(backgroundBrush, path);
            }

            // Draw menu border
            using (var borderPen = new Pen(Color.FromArgb(255, 0, 120, 215), 2))
            using (var path = GetRoundedRectanglePath(menuPosition.X, menuPosition.Y, menuWidth, menuHeight, 10))
            {
                g.DrawPath(borderPen, path);
            }

            
            using (var titleFont = new Font("Segoe UI", 24, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.White))
            {
                g.DrawString("Lurid.Dev", titleFont, titleBrush, menuPosition.X + 15, menuPosition.Y + 15);
            }

            
            DrawTab(g, "Visuals", menuPosition.X + 20, menuPosition.Y + 60, currentTab == MenuTab.Visuals);
            DrawTab(g, "Aimbot", menuPosition.X + 120, menuPosition.Y + 60, currentTab == MenuTab.Aimbot);
            DrawTab(g, "Config", menuPosition.X + 220, menuPosition.Y + 60, currentTab == MenuTab.Config);
            DrawTab(g, "Colors", menuPosition.X + 320, menuPosition.Y + 60, currentTab == MenuTab.Colors);

            
            switch (currentTab)
            {
                case MenuTab.Visuals:
                    DrawVisualsTab(g, menuPosition.X, menuPosition.Y);
                    break;
                case MenuTab.Aimbot:
                    DrawAimbotTab(g, menuPosition.X, menuPosition.Y);
                    break;
                case MenuTab.Config:
                    DrawConfigTab(g, menuPosition.X, menuPosition.Y);
                    break;
                case MenuTab.Colors:
                    DrawColorsTab(g, menuPosition.X, menuPosition.Y);
                    break;
            }
        }

        private static void DrawTab(Graphics g, string text, int x, int y, bool isActive)
        {
            using (var font = new Font("Segoe UI", 12, FontStyle.Bold))
            using (var textBrush = new SolidBrush(isActive ? Color.White : Color.Gray))
            using (var underlinePen = new Pen(isActive ? Color.White : Color.Transparent, 2))
            {
                g.DrawString(text, font, textBrush, x, y);
                g.DrawLine(underlinePen, x, y + 20, x + 60, y + 20);
            }
        }

        private static void DrawVisualsTab(Graphics g, int menuX, int menuY)
        {
            DrawMenuItem(g, "ESP Boxes", menuX + 20, menuY + 100, ShowESPBoxes);
            DrawMenuItem(g, "Health Bars", menuX + 20, menuY + 140, ShowHealthBars);
            DrawMenuItem(g, "Bone ESP", menuX + 20, menuY + 180, ShowBoneESP);

            
            if (showBoneThicknessSlider)
            {
                DrawSlider(g, "Thickness", menuX + 20, menuY + 220, BoneThickness, 0.5f, 3.0f);
            }
        }

        private static void DrawAimbotTab(Graphics g, int menuX, int menuY)
        {
            
            DrawMenuItem(g, "Aimbot", menuX + 20, menuY + 100, false);
            DrawMenuItem(g, "FOV", menuX + 20, menuY + 140, false);
            DrawMenuItem(g, "Smooth", menuX + 20, menuY + 180, false);
        }

        private static void DrawConfigTab(Graphics g, int menuX, int menuY)
        {
            // Add config settings here
            DrawMenuItem(g, "Save Config", menuX + 20, menuY + 100, false);
            DrawMenuItem(g, "Load Config", menuX + 20, menuY + 140, false);
            DrawMenuItem(g, "Reset", menuX + 20, menuY + 180, false);
        }

        private static void DrawColorsTab(Graphics g, int menuX, int menuY)
        {
            
            DrawColorPicker(g, "Enemy Color", menuX + 20, menuY + 100, ref EnemyColor, ref showEnemyColorPicker);
            DrawColorPicker(g, "Friendly Color", menuX + 20, menuY + 150, ref FriendlyColor, ref showFriendlyColorPicker);
            DrawColorPicker(g, "Friendly Line Color", menuX + 20, menuY + 200, ref FriendlyLineColor, ref showFriendlyLineColorPicker);
        }

        private static void DrawColorPicker(Graphics g, string label, int x, int y, ref Color color, ref bool showPicker)
        {
            // Draw label
            g.DrawString(label, font, Brushes.White, x, y);
            
            // Draw color preview box
            Rectangle colorBoxRect = new Rectangle(x + 150, y, 50, 20);
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, colorBoxRect);
            }
            g.DrawRectangle(Pens.White, colorBoxRect);

            // Handle click to show color picker
            Point mousePos = Control.MousePosition;
            if ((GetAsyncKeyState(0x01) & 0x8000) != 0 && 
                (DateTime.Now - lastClickTime).TotalMilliseconds > 200)
            {
                if (colorBoxRect.Contains(mousePos))
                {
                    showPicker = !showPicker;
                    lastClickTime = DateTime.Now;
                }
            }

            // Draw color picker if visible
            if (showPicker)
            {
                int pickerSize = 120; // Smaller size
                int pickerX = x + 250; // Move the color wheel further to the right
                int pickerY = y + 30;  // Keep the same vertical position
                int radius = pickerSize / 2;
                Point center = new Point(pickerX + radius, pickerY + radius);

                // Draw color wheel
                using (Bitmap colorWheel = new Bitmap(pickerSize, pickerSize))
                {
                    for (int i = 0; i < pickerSize; i++)
                    {
                        for (int j = 0; j < pickerSize; j++)
                        {
                            float dx = i - radius;
                            float dy = j - radius;
                            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                            if (distance <= radius)
                            {
                                float angle = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
                                if (angle < 0) angle += 360;
                                
                                float hue = angle / 360f;
                                float saturation = distance / radius;
                                colorWheel.SetPixel(i, j, ColorFromHSL(hue, saturation, 0.5f));
                            }
                        }
                    }
                    g.DrawImage(colorWheel, pickerX, pickerY);
                }

                
                float[] hsl = ColorToHSL(color);
                float selectorAngle = hsl[0] * 360;
                float selectorRadius = hsl[1] * radius;

                Point selectorPos = new Point(
                    center.X + (int)(selectorRadius * Math.Cos(selectorAngle * Math.PI / 180)),
                    center.Y + (int)(selectorRadius * Math.Sin(selectorAngle * Math.PI / 180))
                );

                using (var selectorPen = new Pen(Color.White, 2))
                {
                    g.DrawEllipse(selectorPen, selectorPos.X - 4, selectorPos.Y - 4, 8, 8);
                }

                
                Rectangle pickerRect = new Rectangle(pickerX, pickerY, pickerSize, pickerSize);
                if ((GetAsyncKeyState(0x01) & 0x8000) != 0) 
                {
                    if (pickerRect.Contains(mousePos))
                    {
                        Point relativePos = new Point(
                            mousePos.X - center.X,
                            mousePos.Y - center.Y);

                        float distance = (float)Math.Sqrt(relativePos.X * relativePos.X + relativePos.Y * relativePos.Y);
                        if (distance <= radius)
                        {
                            float angle = (float)(Math.Atan2(relativePos.Y, relativePos.X) * 180 / Math.PI);
                            if (angle < 0) angle += 360;
                            
                            float hue = angle / 360f;
                            float saturation = Math.Min(distance / radius, 1.0f);
                            color = ColorFromHSL(hue, saturation, 0.5f);
                        }
                    }
                    else if (!colorBoxRect.Contains(mousePos))
                    {
                        
                        showPicker = false;
                    }
                }
            }
        }

        
        private static float[] ColorToHSL(Color color)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float h, s, l = (max + min) / 2f;

            if (max == min)
            {
                h = s = 0; 
            }
            else
            {
                float d = max - min;
                s = l > 0.5f ? d / (2f - max - min) : d / (max + min);
                
                if (max == r)
                    h = (g - b) / d + (g < b ? 6f : 0f);
                else if (max == g)
                    h = (b - r) / d + 2f;
                else
                    h = (r - g) / d + 4f;
                
                h /= 6f;
            }

            return new float[] { h, s, l };
        }

        private static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => Color.FromArgb(v, t, p),
                1 => Color.FromArgb(q, v, p),
                2 => Color.FromArgb(p, v, t),
                3 => Color.FromArgb(p, q, v),
                4 => Color.FromArgb(t, p, v),
                _ => Color.FromArgb(v, p, q),
            };
        }

        private static Color ColorFromHSL(float hue, float saturation, float lightness)
        {
            if (saturation == 0)
            {
                return Color.FromArgb(255, (int)(lightness * 255), (int)(lightness * 255));
            }
            else
            {
                float q = lightness < 0.5 ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
                float p = 2 * lightness - q;
                float r = HueToRGB(p, q, hue + 1.0f / 3.0f);
                float g = HueToRGB(p, q, hue);
                float b = HueToRGB(p, q, hue - 1.0f / 3.0f);
                return Color.FromArgb(255, (int)(r * 255), (int)(g * 255), (int)(b * 255));
            }
        }

        private static float HueToRGB(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0f / 6.0f) return p + (q - p) * 6 * t;
            if (t < 1.0f / 2.0f) return q;
            if (t < 2.0f / 3.0f) return p + (q - p) * (2.0f / 3.0f - t) * 6;
            return p;
        }

        private static void HandleColorWheelClick(int relativeX, int relativeY, ref Color color)
        {
            const int size = 150;
            const int wheelX = 180;
            const int wheelY = 50;

            if (relativeX >= wheelX && relativeX <= wheelX + size &&
                relativeY >= wheelY && relativeY <= wheelY + size)
            {
                int x = relativeX - wheelX;
                int y = relativeY - wheelY;

                float hue = (float)Math.Atan2(y - size/2, x - size/2) / (2 * (float)Math.PI);
                float saturation = (float)Math.Sqrt(Math.Pow(y - size/2, 2) + Math.Pow(x - size/2, 2)) / (size/2);
                if (saturation > 1) saturation = 1;

                color = ColorFromHSV(hue * 360, saturation, 1);
            }
        }

        private static void DrawMenuItem(Graphics g, string text, int x, int y, bool isChecked)
        {
            using (var font = new Font("Segoe UI", 14))
            using (var textBrush = new SolidBrush(Color.White))
            using (var checkBoxBrush = new SolidBrush(Color.FromArgb(255, 0, 120, 215)))
            {
                
                int boxSize = 20;
                g.FillRectangle(isChecked ? checkBoxBrush : Brushes.Transparent, x, y, boxSize, boxSize);
                g.DrawRectangle(Pens.White, x, y, boxSize, boxSize);

                
                g.DrawString(text, font, textBrush, x + boxSize + 10, y);
            }
        }

        private static System.Drawing.Drawing2D.GraphicsPath GetRoundedRectanglePath(int x, int y, int width, int height, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawBox(int x, int y, int width, int height, Color color)
        {
            try
            {
                IntPtr pen = CreatePen(0, 3, ColorToRGB(color));
                IntPtr oldPen = SelectObject(desktopDC, pen);

                Rectangle(desktopDC, x, y, x + width, y + height);

                SelectObject(desktopDC, oldPen);
                DeleteObject(pen);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DrawBox: {ex.Message}");
            }
        }

        public static void DrawFilledBox(int x, int y, int width, int height, Color color)
        {
            try
            {
                IntPtr brush = CreateSolidBrush(ColorToRGB(color));
                IntPtr oldBrush = SelectObject(desktopDC, brush);

                Rectangle(desktopDC, x, y, x + width, y + height);

                SelectObject(desktopDC, oldBrush);
                DeleteObject(brush);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DrawFilledBox: {ex.Message}");
            }
        }

        public static void DrawText(string text, int x, int y, Color color)
        {
            try
            {
                SetBkMode(desktopDC, 1);
                SetTextColor(desktopDC, ColorToRGB(color));
                TextOutW(desktopDC, x, y, text, text.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DrawText: {ex.Message}");
            }
        }

        private static int ColorToRGB(Color color)
        {
            return (color.R | (color.G << 8) | (color.B << 16));
        }

        private static void DrawFilledRoundedBox(int x, int y, int width, int height, int radius, Color color)
        {
            
        }

        private static void DrawRoundedBox(int x, int y, int width, int height, int radius, Color color)
        {
            
        }

        private static void UpdateSnow()
        {
            const int menuWidth = 300;
            const int menuHeight = 400;
            const int menuX = 50;
            const int menuY = 50;

            foreach (var flake in snowflakes)
            {
                flake.Y += flake.Speed;
                if (flake.Y > menuY + menuHeight)
                {
                    flake.Y = menuY - flake.Size;
                    flake.X = random.Next(menuX, menuX + menuWidth);
                }
            }
        }

        private static void DrawSlider(Graphics g, string label, int x, int y, float value, float min, float max)
        {
            const int sliderWidth = 200;
            const int handleSize = 10;

            
            using (var font = new Font("Segoe UI", 12))
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(label, font, brush, x, y);
            }

            
            using (var trackBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
            {
                g.FillRectangle(trackBrush, x, y + 30, sliderWidth, 10);
            }

            
            float normalizedValue = (value - min) / (max - min);
            int handleX = x + (int)(normalizedValue * sliderWidth);

            
            using (var handleBrush = new SolidBrush(Color.FromArgb(255, 0, 120, 215)))
            {
                g.FillRectangle(handleBrush, handleX - handleSize/2, y + 25, handleSize, handleSize * 2);
            }
        }

        private static void HandleSliderInteraction(int relativeX, int relativeY, int x, int y, ref float value, float min, float max)
        {
            const int sliderWidth = 200;
            const int handleSize = 10;

            
            if (relativeX >= x && relativeX <= x + sliderWidth &&
                relativeY >= y + 25 && relativeY <= y + 25 + handleSize * 2)
            {
                if ((GetAsyncKeyState(0x01) & 0x8000) != 0) 
                {
                    isDraggingSlider = true;
                }
            }

            if (isDraggingSlider)
            {
                
                float newValue = min + (max - min) * ((relativeX - x) / (float)sliderWidth);
                value = Math.Clamp(newValue, min, max);

                if ((GetAsyncKeyState(0x01) & 0x8000) == 0) 
                {
                    isDraggingSlider = false;
                }
            }
        }
    }
}