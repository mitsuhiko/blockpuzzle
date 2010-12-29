//  Copyright (c) 2010, Armin Ronacher
//  Licensed under the BSD license, see LICENSE for more information

using System;
using System.Diagnostics;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace BlockPuzzle {

    /// <summary>
    /// Represents the game block information.  This is currently the texture,
    /// color and the information if this block is hard or not.
    /// </summary>
    public sealed class GameBlock {
        private Texture texture;
        private Color4 color;
        private bool hard;

        public GameBlock(Texture texture, Color4 color, bool hard)
        {
            this.texture = texture;
            this.color = color;
            this.hard = hard;
        }

        public void Draw(DrawContext ctx, GamePane pane, float alpha)
        {
            Color4 blockColor = color;
            blockColor.A = alpha;
            ctx.BindTexture(texture);
            ctx.SetColor(blockColor);
            ctx.DrawTexturedRectangle(pane.BlockSize, pane.BlockSize, texture);
        }

        public Texture Texture {
            get { return texture; }
        }
        public Color4 Color {
            get { return color; }
        }
        public bool Hard {
            get { return hard; }
        }
    }

    /// <summary>
    /// Represents a block based animation on top of the game pane.  This
    /// is intended to be used for animations that are not associated to
    /// a life block.
    /// </summary>
    public abstract class GameBlockAnimation {
        private GameBlock block;
        private GamePane pane;
        private int column;
        private int row;

        public GameBlockAnimation(GameBlock block, GamePane pane, int column, int row)
        {
            this.block = block;
            this.pane = pane;
            this.column = column;
            this.row = row;
        }

        public abstract bool Update(FrameEventArgs args);
        public abstract void Draw(DrawContext ctx);

        public virtual void MoveUp()
        {
            row++;
        }

        public GameBlock Block {
            get { return block; }
        }
        public GamePane Pane {
            get { return pane; }
        }
        public int Column {
            get { return column; }
        }
        public int Row {
            get { return row; }
        }
    }

    /// <summary>
    /// Animation for disappearing blocks.
    /// </summary>
    public class GameBlockDissolveAnimation : GameBlockAnimation {
        private float scale;
        private float rotation;

        public GameBlockDissolveAnimation(GameBlock block, GamePane pane, int column, int row)
            : base(block, pane, column, row)
        {
            this.scale = 1.0f;
            this.rotation = 0.0f;
        }

        public override bool Update(FrameEventArgs args)
        {
            scale -= (float)Math.Pow(args.Time, 1.1) * 5.0f;
            rotation += (float)args.Time * 180.0f;
            return scale > 0.0f;
        }

        public override void Draw(DrawContext ctx)
        {
            float center = Pane.BlockSize / 2.0f;
            ctx.Push();
            ctx.Translate(Pane.GetBlockX(Column), Pane.GetBlockY(Row));
            ctx.ScaleAroundPoint(scale, scale, center, center);
            ctx.RotateAroundPoint(rotation, center, center);
            Block.Draw(ctx, Pane, 1.0f);
            ctx.Pop();
        }
    }

    /// <summary>
    /// An animation for blocks that are in the process of exploding.  This
    /// is used for hard blocks that became soft blocks.
    /// </summary>
    public class GameBlockExplosionAnimation : GameBlockAnimation {
        private float scale;
        private float rotation;
        private float alpha;
        private float yOff;

        public GameBlockExplosionAnimation(GameBlock block, GamePane pane, int column, int row)
            : base(block, pane, column, row)
        {
            scale = 1.0f;
            rotation = 0.0f;
            alpha = 1.0f;
            yOff = 0.0f;
        }

        public override bool Update(FrameEventArgs args)
        {
            alpha -= (float)Math.Pow(args.Time, 1.15) * 3.0f;
            rotation += (float)args.Time * 180.0f;
            scale += 1.7f * (float)args.Time;
            yOff -= 60.0f * (float)args.Time;
            return alpha > 0.0f;
        }

        public override void Draw (DrawContext ctx)
        {
            float center = Pane.BlockSize / 2.0f;
            ctx.Push();
            ctx.Translate(Pane.GetBlockX(Column), Pane.GetBlockY(Row) + yOff);
            ctx.ScaleAroundPoint(scale, scale, center, center);
            ctx.RotateAroundPoint(rotation, center, center);
            Block.Draw(ctx, Pane, alpha);
            ctx.Pop();
        }
    }

    /// <summary>
    /// Stores the state for a block in the raster.
    /// </summary>
    public sealed class GameBlockState {
        private GameBlock block;
        private GamePane pane;
        private int column;
        private int row;
        private float jiggle;
        private float animationSourceX;
        private float animationSourceY;
        private float animationStep;
        private float animationSpeedFactor;

        public GameBlockState(GamePane pane, int column, int row, float jiggle)
        {
            this.block = null;
            this.pane = pane;
            this.column = column;
            this.row = row;
            this.jiggle = jiggle;
            this.animationStep = -1.0f;
            this.animationSpeedFactor = 1.0f;
        }

        /// <summary>
        /// Changes a block state's block with the one from another block state.
        /// The exchange happens with a little animation and afterwards both
        /// will also have the jiggle swapped.  These two blocks must be on the
        /// same row though.
        /// </summary>
        public void ExchangeWith(GameBlockState other)
        {
            Debug.Assert(other.row == row);
            SwapContentsWith(other);
            animationStep = other.animationStep = 0.0f;
            animationSpeedFactor = other.animationSpeedFactor = 7.0f;
            animationSourceX = other.BlockX;
            animationSourceY = other.BlockY;
            other.animationSourceX = BlockX;
            other.animationSourceY = BlockY;
        }

        /// <summary>
        /// Like ExchangeWith but without the block animation.  Used to move
        /// up blocks when a new row appears.
        /// </summary>
        public void SwapWith(GameBlockState other)
        {
            GameBlockState clone = SwapContentsWith(other);
            animationSourceX = other.animationSourceX;
            other.animationSourceX = clone.animationSourceX;
            animationSourceY = other.animationSourceY;
            other.animationSourceY = clone.animationSourceY;
            animationStep = other.animationStep;
            other.animationStep = clone.animationStep;
        }

        /// <summary>
        /// Special version of swapping.  Basically does a half-ass swap with
        /// the upper cell.
        /// </summary>
        public void MoveUp()
        {
            SwapContentsWith(pane[Column, Row + 1]);
            animationSourceY += pane.BlockSize;
        }

        /// <summary>
        /// Like MoveUp() but to a lower cell that is empty.
        /// </summary>
        public void MoveDown(GameBlockState lowerState)
        {
            Debug.Assert(lowerState.Empty, "Can only move down to empty states");
            SwapContentsWith(lowerState);
            lowerState.animationSourceX = BlockX;
            lowerState.animationSourceY = BlockY;
            lowerState.animationStep = 0.0f;
            lowerState.animationSpeedFactor = 50.0f / (Row - lowerState.Row);
        }

        /// <summary>
        /// Explodes this hard block and replaces it with a new soft block.
        /// </summary>
        public void MakeSoftBlock()
        {
            Debug.Assert(Hard, "can only make hard blocks soft");
            pane.AddAnimation(new GameBlockExplosionAnimation(block, pane, Column, Row));
            block = pane.GetRandomBlock();
        }

        /// <summary>
        /// Internal helper for swapping.
        /// </summary>
        private GameBlockState SwapContentsWith(GameBlockState other)
        {
            GameBlockState clone = (GameBlockState)MemberwiseClone();
            block = other.block;
            other.block = clone.block;
            jiggle = other.jiggle;
            other.jiggle = clone.jiggle;
            return clone;
        }

        /// <summary>
        /// If a block based animation is active (transitions only) we update this.
        /// This is *not* a GameBlockAnimation which is used for block animations that
        /// are not stored in a game block state.
        /// </summary>
        public void UpdateAnimation(FrameEventArgs args)
        {
            if (animationStep < 0.0f)
                return;
            animationStep += (float)args.Time * animationSpeedFactor;
            if (animationStep >= 1.0f)
                animationStep = -1.0f;
        }

        /// <summary>
        /// Clears the block and makes it dissolve.
        /// </summary>
        public void Clear()
        {
            if (Empty)
                return;
            pane.AddAnimation(new GameBlockDissolveAnimation(block, pane, column, row));
            block = null;
        }

        /// <summary>
        /// Clears the block and makes it explode.
        /// </summary>
        public void Explode()
        {
            if (Empty)
                return;
            pane.AddAnimation(new GameBlockExplosionAnimation(block, pane, column, row));
            block = null;
        }

        public void Draw(DrawContext ctx)
        {
            if (Empty)
                return;
            float xOff = 0.0f;
            float x, y;
            bool underCursor = false;
            if (!Hard && row == pane.CursorRow) {
                if (column == pane.CursorColumn) {
                    underCursor = true;
                    xOff = -3.0f;
                } else if (column == pane.CursorColumn + 1) {
                    underCursor = true;
                    xOff = 3.0f;
                }
            }

            float angle = underCursor
                ? pane.Jitter * 30.0f
                : (float)Math.Sin(pane.Jitter * jiggle) * 3.0f;

            ctx.Push();
            if (animationStep >= 0.0f) {
                x = animationSourceX + (BlockX - animationSourceX) * animationStep;
                y = animationSourceY + (BlockY - animationSourceY) * animationStep;
            } else {
                x = BlockX;
                y = BlockY;
            }
            ctx.Translate(x + xOff, y);
            ctx.RotateAroundPoint(angle, pane.BlockSize / 2, pane.BlockSize / 2);
            if (underCursor) {
                float scale = 1.35f * (1.0f - (float)Math.Sin(pane.Jitter * 3.0f) * 0.06f);
                ctx.ScaleAroundPoint(scale, scale, pane.BlockSize / 2, pane.BlockSize / 2);
            }
            block.Draw(ctx, pane, 1.0f);
            ctx.Pop();
        }

        public bool Animated {
            get { return animationStep >= 0.0f; }
        }
        public bool UnderCursor {
            get {
                return (row == pane.CursorRow && column >= pane.CursorColumn &&
                        column <= pane.CursorColumn + 1);
            }
        }
        public bool DrawDeferred {
            get { return UnderCursor; }
        }
        public int Column {
            get { return column; }
        }
        public int Row {
            get { return row; }
        }
        public float BlockX {
            get { return pane.GetBlockX(column); }
        }
        public float BlockY {
            get { return pane.GetBlockY(row); }
        }
        public bool Empty {
            get { return block == null; }
        }
        public GameBlock Block {
            get { return block; }
            set { block = value; }
        }
        public bool Hard {
            get { return block != null && block.Hard; }
        }
    }
}

