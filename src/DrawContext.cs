//  Copyright (c) 2010, Armin Ronacher
//  Licensed under the BSD license, see LICENSE for more information
using System;
using System.Drawing;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace BlockPuzzle {

    /// <summary>
    /// Helper class that provides functions to draw things.  Right now this uses
    /// a fixed function pipeline for OpenGL but it exists so that it would be
    /// possible to easily switch to a programmable pipeline if this would become
    /// necessary.
    /// </summary>
    public class DrawContext {
        private Stack<uint> boundTextures;

        public DrawContext()
        {
            boundTextures = new Stack<uint>();
            boundTextures.Push(0);
        }

        public void BindTexture(Texture texture)
        {
            if (boundTextures.Peek() == texture.ID)
                return;
            GL.BindTexture(TextureTarget.Texture2D, texture.ID);
            boundTextures.Pop();
            boundTextures.Push(texture.ID);
        }

        public void UnbindTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
            boundTextures.Pop();
            boundTextures.Push(0);
        }

        public void Push()
        {
            GL.PushMatrix();
            GL.PushAttrib(AttribMask.AllAttribBits);
            boundTextures.Push(boundTextures.Peek());
        }

        public void Pop()
        {
            boundTextures.Pop();
            GL.PopAttrib();
            GL.PopMatrix();
        }

        public void Translate(float x, float y)
        {
            GL.Translate(x, y, 0.0f);
        }

        public void Translate(Vector2 vec)
        {
            GL.Translate(vec.X, vec.Y, 0.0f);
        }

        public void Scale(float x, float y)
        {
            GL.Scale(x, y, 1.0f);
        }

        public void ScaleAroundPoint(float x, float y, float px, float py)
        {
            Translate(px, py);
            Scale(x, y);
            Translate(-px, -py);
        }

        public void ScaleAroundPoint(float x, float y, ref Vector2 pos)
        {
            ScaleAroundPoint(x, y, pos.X, pos.Y);
        }

        public void Rotate(float angle)
        {
            GL.Rotate(angle, 0.0f, 0.0f, 1.0f);
        }

        public void RotateAroundPoint(float angle, float x, float y)
        {
            Translate(x, y);
            Rotate(angle);
            Translate(-x, -y);
        }

        public void RotateAroundPoint(float angle, ref Vector2 pos)
        {
            RotateAroundPoint(angle, pos.X, pos.Y);
        }

        public void SetColor(Color4 color)
        {
            GL.Color4(color);
        }

        public void UnsetColor()
        {
            GL.Color4(255, 255, 255, 255);
        }

        /// <summary>
        /// Draws a rectangle with the given dimensions but will not emit any
        /// texture coordinates.
        /// </summary>
        public void DrawRectangle(float width, float height)
        {
            // TODO: consider switching to vertex arrays
            GL.Begin(BeginMode.Quads);
            GL.Vertex3(0.0f, 0.0f, 0.0f);
            GL.Vertex3(width, 0.0f, 0.0f);
            GL.Vertex3(width, height, 0.0f);
            GL.Vertex3(0.0f, height, 0.0f);
            GL.End();
        }

        /// <summary>
        /// Like DrawRectangle but will also emit texture coordinates for the texture
        /// given.  This however will not bind the texture.  This has to happen
        /// separately upfront.  The reason for this is that texture switches are
        /// quite expensive and it makes sense to not bind them every time you draw
        /// if there are better ways to handle that.
        /// </summary>
        public void DrawTexturedRectangle(float width, float height, Texture tex)
        {
            // TODO: consider switching to vertex arrays
            float fac_x = (float)tex.Width / tex.StoredWidth;
            float fac_y = (float)tex.Height / tex.StoredHeight;
            float off_x = (float)tex.OffsetX / tex.StoredWidth;
            float off_y = (float)tex.OffsetY / tex.StoredHeight;

            GL.Begin(BeginMode.Quads);
            GL.TexCoord2(off_x, off_y);
            GL.Vertex3(0.0f, 0.0f, 0.0f);
            GL.TexCoord2(off_x + fac_x, off_y);
            GL.Vertex3(width, 0.0f, 0.0f);
            GL.TexCoord2(off_x + fac_x, off_y + fac_y);
            GL.Vertex3(width, height, 0.0f);
            GL.TexCoord2(off_x, off_y + fac_y);
            GL.Vertex3(0.0f, height, 0.0f);
            GL.End();
        }
    }
}

