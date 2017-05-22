using Corale.Colore.Core;
using Corale.Colore.Razer.Keyboard;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Color = Corale.Colore.Core.Color;

namespace WindowsFormsChromaMambaImage
{
    public partial class Form1 : Form
    {
        private const string KEY_CHROMA_MAMBA_IMAGE = "CHROMA_MAMBA_IMAGE";
        private const string KEY_IMAGE = "IMAGE";

        private bool _mLoadingTexture = false;
        private string _mFileName = string.Empty;

        private int _mMinX = 0;
        private int _mMinY = 0;
        private int _mMaxX = 0;
        private int _mMaxY = 0;

        /// <summary>
        /// Custom effects can set individual LEDs on Mouse
        /// </summary>
        private static Corale.Colore.Razer.Mouse.Effects.Custom _mMouseCustomEffect =
            Corale.Colore.Razer.Mouse.Effects.Custom.Create();

        private class KeyData
        {
            public int _mIndex;
            public Color _mColor;
            public static implicit operator KeyData(int index)
            {
                KeyData keyData = new KeyData();
                keyData._mIndex = index;
                keyData._mColor = Color.Black;
                return keyData;
            }
        }

        #region Key layout

        private static KeyData[,] _sKeys =
        {
            {
                2, 0, 1, 11,
            },
            {
                3, -1, -1, 12,
            },
            {
                4, -1, -1, 13,
            },
            {
                5, -1, -1, 14,
            },
            {
                6, -1, -1, 15,
            },
            {
                7, -1, -1, 16,
            },
            {
                8, -1, -1, 17,
            },
            {
                9, -1, -1, -1,
            },
            {
                10, -1, -1, -1,
            },
        };

        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        private void _mButtonQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void LoadImage()
        {
            if (null != _mPicture.Image)
            {
                _mPicture.Image.Dispose();
            }

            if (!File.Exists(_mFileName))
            {
                return;
            }

            _mPicture.Image = Image.FromFile(_mFileName);

            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(KEY_CHROMA_MAMBA_IMAGE);
            key.SetValue(KEY_IMAGE, _mFileName);
            key.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _mPicture.SizeMode = PictureBoxSizeMode.StretchImage;

            Microsoft.Win32.RegistryKey key;
            foreach (string name in Microsoft.Win32.Registry.CurrentUser.GetSubKeyNames())
            {
                if (name == KEY_CHROMA_MAMBA_IMAGE)
                {
                    key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KEY_CHROMA_MAMBA_IMAGE);
                    if (null != key)
                    {
                        _mFileName = (string)key.GetValue(KEY_IMAGE);
                        LoadImage();
                        ResetMarquee();
                    }
                }
            }

            DisplayImageOnFirefly();

            _mPicture.MouseDown += _mPicture_MouseDown;
            _mPicture.MouseUp += _mPicture_MouseUp;

            _mTimerAnimation.Start();

            SetColor(Color.Green);
        }

        private void _mPicture_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Image image = _mPicture.Image;
            if (null == image)
            {
                return;
            }

            Bitmap bitmap = image as Bitmap;
            if (null == bitmap)
            {
                return;
            }

            int minX = (int)(e.X / (float)_mPicture.Width * bitmap.Width);
            int minY = (int)(e.Y / (float)_mPicture.Height * bitmap.Height);
            if (minX < bitmap.Width &&
                minY < bitmap.Height)
            {
                _mMinX = minX;
                _mMinY = minY;
            }
            else
            {
                return;
            }

            /*
            bitmap.SetPixel(_mMinX, _mMinY, System.Drawing.Color.Green);
            _mPicture.Image = bitmap;
            */

            return;
        }

        private void _mPicture_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            LoadImage();

            Image image = _mPicture.Image;
            if (null == image)
            {
                return;
            }

            Bitmap bitmap = image as Bitmap;
            if (null == bitmap)
            {
                return;
            }

            MouseEventArgs evt = e as MouseEventArgs;
            int x = (int)(evt.X / (float)_mPicture.Width * bitmap.Width);
            int y = (int)(evt.Y / (float)_mPicture.Height * bitmap.Height);

            int minX = Math.Min(_mMinX, x);
            if (minX < 0 || minX >= bitmap.Width)
            {
                return;
            }

            int minY = Math.Min(_mMinY, y);
            if (minY < 0 || minY >= bitmap.Height)
            {
                return;
            }

            int maxX = Math.Max(_mMinX, x);
            if (maxX < 0 || maxX >= bitmap.Width)
            {
                return;
            }

            int maxY = Math.Max(_mMinY, y);
            if (maxY < 0 || maxY >= bitmap.Height)
            {
                return;
            }

            if (minX == maxX ||
                minY == maxY)
            {
                minX = 0;
                minY = 0;
                maxX = bitmap.Width - 1;
                maxY = bitmap.Height - 1;
            }

            _mMinX = minX;
            _mMinY = minY;
            _mMaxX = maxX;
            _mMaxY = maxY;

            // invert outside area
            for (x = 0; x < bitmap.Width; ++x)
            {
                for (y = 0; y < bitmap.Height; ++y)
                {
                    if (x < minX || x > maxX ||
                        y < minY || y > maxY)
                    {
                        System.Drawing.Color c = bitmap.GetPixel(x, y);
                        c = System.Drawing.Color.FromArgb(c.A, 255 - c.R, 255 - c.G, 255 - c.B);
                        bitmap.SetPixel(x, y, c);
                    }
                }
            }

            _mPicture.Image = bitmap;

            /*
            bitmap.SetPixel(x, y, System.Drawing.Color.Red);
            _mPicture.Image = bitmap;
            */

            DisplayImageOnFirefly();
        }

        /// <summary>
        /// Set all the LEDs as the same color
        /// </summary>
        /// <param name="color"></param>
        static void SetColor(Corale.Colore.Core.Color color)
        {
            for (int i = 0; i < Corale.Colore.Razer.Mouse.Constants.MaxLeds; ++i)
            {
                _mMouseCustomEffect[i] = color;
            }
            Corale.Colore.Core.Mouse.Instance.SetCustom(_mMouseCustomEffect);
        }

        static void SetColor(int index, Corale.Colore.Core.Color color)
        {
            if (index < 0)
            {
                return;
            }
            _mMouseCustomEffect[index] = color;
            Corale.Colore.Core.Mouse.Instance.SetCustom(_mMouseCustomEffect);
        }

        private void DisplayImageOnFirefly()
        {
            if (null == _sKeys)
            {
                return;
            }

            Image image = _mPicture.Image;
            if (null == image)
            {
                return;
            }

            Bitmap bitmap = image as Bitmap;
            if (null == bitmap)
            {
                return;
            }

            if (string.IsNullOrEmpty(_mFileName))
            {
                return;
            }

            if (_mMinX == _mMaxX ||
                _mMinY == _mMaxY)
            {
                _mMinX = 0;
                _mMinY = 0;
                _mMaxX = bitmap.Width - 1;
                _mMaxY = bitmap.Height - 1;
            }

            for (int i = 0; i < _sKeys.GetLength(0); ++i)
            {
                float ratioI = i / (float)_sKeys.GetLength(0);
                int y = _mMinY + (int)(ratioI * (_mMaxY - _mMinY));
                for (int j = 0; j < _sKeys.GetLength(1); ++j)
                {
                    float ratioJ = j / (float)_sKeys.GetLength(1);
                    int x = _mMinX + (int)(ratioJ * (_mMaxX - _mMinX));
                    System.Drawing.Color color = System.Drawing.Color.Black;
                    if (x < bitmap.Width &&
                        y < bitmap.Height)
                    {
                        color = bitmap.GetPixel(x, y);
                    }
                    KeyData keyData = _sKeys[i, j];
                    keyData._mColor = new Color(color.R, color.G, color.B);
                    SetColor(keyData._mIndex, keyData._mColor);
                }
            }
        }

        private void _mTimerAnimation_Tick(object sender, EventArgs e)
        {
            if (_mLoadingTexture)
            {
                return;
            }
            DisplayImageOnFirefly();
        }

        private void ResetMarquee()
        {
            Image image = _mPicture.Image;
            if (null == image)
            {
                return;
            }

            Bitmap bitmap = image as Bitmap;
            if (null == bitmap)
            {
                return;
            }

            _mMinX = 0;
            _mMinY = 0;
            _mMaxX = bitmap.Width - 1;
            _mMaxY = bitmap.Height - 1;
        }

        private void _mButtonLoad_Click(object sender, EventArgs e)
        {
            _mLoadingTexture = true;

            if (null != _mPicture.Image)
            {
                _mPicture.Image.Dispose();
                _mPicture.Image = null;
            }

            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "image files (*.jpg;*.gif)|*.jpg;*.gif";
            if (string.IsNullOrEmpty(_mFileName))
            {
                openFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
                openFileDialog1.FileName = string.Empty;
            }
            else
            {
                openFileDialog1.FileName = _mFileName;
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(_mFileName);
            }

            try
            {
                _mFileName = null;
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    _mFileName = openFileDialog1.FileName;
                    LoadImage();

                    ResetMarquee();

                    DisplayImageOnFirefly();
                }
            }
            catch (ArgumentException)
            {
                _mFileName = null;
            }

            _mLoadingTexture = false;
        }
    }
}
