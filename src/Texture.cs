//  Copyright (c) 2010, Armin Ronacher
//  Licensed under the BSD license, see LICENSE for more information

using System;
using System.Drawing;
using System.Drawing.Imaging;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace BlockPuzzle {

    public class Texture : GraphicsObject {
        private uint id;
        private int width;
        private int height;
        private int storedWidth;
        private int storedHeight;
        protected int offsetX;
        protected int offsetY;

        protected Texture(int width, int height, int storedWidth, int storedHeight,
                          int offsetX, int offsetY)
        {
            this.width = width;
            this.height = height;
            this.storedWidth = storedWidth;
            this.storedHeight = storedHeight;
            this.offsetX = offsetX;
            this.offsetY = offsetY;
        }

        public Texture(string filename)
        {
            try {
                using (Bitmap bitmap = new Bitmap(filename))
                    InitWithBitmap(bitmap);
            } catch (Exception) {
                throw new ApplicationException("Could not load texture '" + filename + "'");
            }
        }

        public Texture(Bitmap bitmap)
        {
            InitWithBitmap(bitmap);
        }

        private void InitWithBitmap(Bitmap bitmap)
        {
            /// TODO: upscale to power of two if necessary
            GL.GenTextures(1, out id);
            GL.BindTexture(TextureTarget.Texture2D, id);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width,
                    data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                    PixelType.UnsignedByte, data.Scan0);
            } finally {
                bitmap.UnlockBits(data);
            }
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                            (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                            (int)TextureMagFilter.Linear);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            width = bitmap.Width;
            height = bitmap.Height;
            storedWidth = bitmap.Width;
            storedHeight = bitmap.Height;
            offsetX = 0;
            offsetY = 0;
        }

        protected override void ReleaseResources()
        {
            // important: use the actual stored id here, not the virtual accessor.
            // otherwise a texture slice would delete the texture of the parent
            // texture object.
            if (id != 0)
                GL.DeleteTexture(id);
        }

        /// <summary>
        /// Returns a new texture slice for this texture.  The returned slice has a
        /// reference to the actual texture and updated texture coordinates.
        /// </summary>
        public TextureSlice Slice(int x, int y, int width, int height)
        {
            return new TextureSlice(ActualTexture, offsetX + x, offsetY + y,
                                    width, height);
        }

        public virtual Texture ActualTexture {
            get { return this; }
        }

        public virtual uint ID {
            get { return id; }
        }

        public int OffsetX {
            get { return offsetX; }
        }

        public int OffsetY {
            get { return offsetY; }
        }

        public int Width {
            get { return width; }
        }

        public int Height {
            get { return height; }
        }

        public int StoredWidth {
            get { return storedWidth; }
        }

        public int StoredHeight {
            get { return storedHeight; }
        }
    }


    public class TextureSlice : Texture {
        private Texture parent;

        public TextureSlice(Texture tex, int offsetX, int offsetY, int width, int height)
            : base(width, height, tex.StoredWidth, tex.StoredHeight, offsetX, offsetY)
        {
            parent = tex;
        }

        public override Texture ActualTexture {
            get { return parent; }
        }

        public override uint ID {
            get { return parent.ID; }
        }
    }
}

