//  Copyright (c) 2010, Armin Ronacher
//  Licensed under the BSD license, see LICENSE for more information

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;


namespace BlockPuzzle {

    public class GamePane {
        private GameSession session;
        private Random rnd;
        private GameBlock[] regularBlocks;
        private GameBlock[] hardBlocks;
        private Texture cursorTexture;
        private float jitter;
        private GameBlockState[,] grid;
        private Vector2 position;
        private HashSet<GameBlockAnimation> activeAnimations;
        private int cursorColumn;
        private int cursorRow;
        private float scrollY;
        private float scrollSpeed;
        private bool fastScrollMode;
        private bool gameOver;

        private class ClearInfo {
            public enum ClearDirection {
                Row,
                Column
            };
            public int Column;
            public int Row;
            public int Count;
            public ClearDirection Direction;

            public ClearInfo(int column, int row, int count, ClearDirection direction)
            {
                Column = column;
                Row = row;
                Count = count;
                Direction = direction;
            }
        }

        public GamePane(GameSession session, GameBlock[] regularBlocks, GameBlock[] hardBlocks,
                        float posX, float posY)
        {
            this.session = session;
            this.regularBlocks = regularBlocks;
            this.hardBlocks = hardBlocks;
            this.rnd = new Random(session.Game.Random.Next());
            grid = new GameBlockState[Columns, Rows];
            position = new Vector2(posX, posY);
            jitter = 0.0f;
            scrollY = 0.0f;
            scrollSpeed = DefaultScrollSpeed;
            cursorColumn = 0;
            cursorRow = 1;
            cursorTexture = regularBlocks[0].Texture;
            activeAnimations = new HashSet<GameBlockAnimation>();

            for (int column = 0; column < Columns; column++)
                for (int row = 0; row < Rows; row++)
                    grid[column, row] = new GameBlockState(this, column, row,
                                                           (float)rnd.NextDouble());

            PopulateWithRandomBlocks(7);
        }

        /// <summary>
        /// Returns a random game block from the available soft
        /// game blocks.
        /// </summary>
        public GameBlock GetRandomBlock()
        {
            return regularBlocks[rnd.Next() % regularBlocks.Length];
        }

        /// <summary>
        /// Populates the game to a given height with random blocks.  These
        /// blocks are placed in a way that they cannot be cleared automatically.
        /// </summary>
        public void PopulateWithRandomBlocks(int height)
        {
            for (int row = 0; row < height; row++)
                InsertNewRow(true);
        }

        /// <summary>
        /// Inserts a new row at the bottom.  This also moves all rows up.
        /// The row placed is always safe row wise which means that they will not
        /// clear itself partially once placed.  Optionally the same can be enabled
        /// for rows which is used to fill the initial space.
        /// </summary>
        public void InsertNewRow(bool safeColumns)
        {
            Color4 lastColor;
            int consecutiveBlocks = 0;
            MoveRowsUp();
            for (int column = 0; column < Columns; column++) {
                GameBlock newBlock = GetRandomBlock();
                while ((newBlock.Color == lastColor && consecutiveBlocks >= 1) ||
                       (safeColumns && !this[column, 1].Empty &&
                        newBlock.Color == this[column, 1].Block.Color))
                    newBlock = GetRandomBlock();
                if (newBlock.Color == lastColor) {
                    consecutiveBlocks++;
                } else {
                    lastColor = newBlock.Color;
                    consecutiveBlocks = 0;
                }
                this[column, 0].Block = newBlock;
            }
            // no point in clearing if safe columns are in use.  We never generate a
            // pattern in rows that could be cleared anyways, and if safe columns are
            // enabled we also won't create patterns that can be cleared the other
            // way round.
            if (!safeColumns)
                TryClear();
        }

        /// <summary>
        /// Drops a new hard line onto the game field and applies gravity.
        /// </summary>
        public void DropHardLine()
        {
            int topRow = Rows - 1;
            GameBlock hardBlock = hardBlocks[rnd.Next() % hardBlocks.Length];
            for (int column = 0; column < Columns; column++)
                this[column, topRow].Block = hardBlock;
            TryClear();
        }

        /// <summary>
        /// Moves all rows up.  Used internally by InsertNewRow().
        /// </summary>
        protected void MoveRowsUp()
        {
            for (int row = Rows - 2; row >= 0; row--)
                for (int column = 0; column < Columns; column++)
                    this[column, row].MoveUp();
            foreach (GameBlockAnimation blockAnimation in activeAnimations)
                blockAnimation.MoveUp();
        }

        /// <summary>
        /// Tests for line clearing.  This also applies gravity as necessary.  Usually
        /// not necessary to call this externally as it happens automatically when
        /// lines are dropped, inserted from the bottom and after each exchange.
        /// This also checks if the game is over.
        /// </summary>
        public void TryClear()
        {
            bool tryClear = true;
            int clearCount = 0;
            int lineCount = 0;
            while (tryClear) {
                List<ClearInfo> toClear = new List<ClearInfo>();

                TryClearRows(toClear);
                TryClearColumns(toClear);
                lineCount += toClear.Count;

                foreach (ClearInfo info in toClear) {
                    switch (info.Direction) {
                    case ClearInfo.ClearDirection.Row:
                        for (var column = info.Column; column < info.Column + info.Count;
                             column++) {
                            this[column, info.Row].Clear();
                            TryBreakHardBlock(column, info.Row + 1);
                        }
                        break;
                    case ClearInfo.ClearDirection.Column:
                        for (var row = info.Row; row < info.Row + info.Count; row++)
                            this[info.Column, row].Clear();
                        TryBreakHardBlock(info.Column, info.Row + info.Count);
                        break;
                    }
                    clearCount += info.Count;
                }

                tryClear = ApplyGravity();
            }

            if (clearCount > 0)
                OnBlocksCleared(clearCount, lineCount);

            CheckForGameOver();
        }

        /// <summary>
        /// Implements the logic to break hard rows into soft blocks.  Hard blocks are
        /// broken up into soft ones when something next to them is cleared.
        /// </summary>
        private void TryBreakHardBlock(int column, int row)
        {
            if (row >= Rows)
                return;
            GameBlockState blockState = this[column, row];
            if (!blockState.Hard)
                return;
            for (int probeColumn = 0; probeColumn < Columns; probeColumn++)
                this[probeColumn, row].MakeSoftBlock();
        }

        /// <summary>
        /// Helper for TryClear() that clears row wise.
        /// </summary>
        private void TryClearRows(List<ClearInfo> toClear)
        {
            for (int row = 1; row < Rows; row++) {
                for (int column = 0; column < Columns - 1; ) {
                    int columnStart = column;
                    GameBlockState thisState = this[column++, row];
                    if (thisState.Empty || thisState.Hard)
                        continue;
                    Color4 color = thisState.Block.Color;
                    int inTheRow = 1;
                    for (; column < Columns; column++, inTheRow++) {
                        GameBlockState otherState = this[column, row];
                        if (otherState.Empty || color != otherState.Block.Color)
                            break;
                    }
                    if (inTheRow >= 3)
                        toClear.Add(new ClearInfo(columnStart, row, inTheRow,
                                                  ClearInfo.ClearDirection.Row));
                }
            }
        }

        /// <summary>
        /// Helper for TryClear() that clears column wise.
        /// </summary>
        private void TryClearColumns(List<ClearInfo> toClear)
        {
            for (int column = 0; column < Columns; column++) {
                for (int row = 1; row < Rows - 1; ) {
                    int rowStart = row;
                    GameBlockState thisState = this[column, row++];
                    if (thisState.Empty || thisState.Hard)
                        continue;
                    Color4 color = thisState.Block.Color;
                    int inTheColumn = 1;
                    for (; row < Rows; row++, inTheColumn++) {
                        GameBlockState otherState = this[column, row];
                        if (otherState.Empty || color != otherState.Block.Color)
                            break;
                    }
                    if (inTheColumn >= 3)
                        toClear.Add(new ClearInfo(column, rowStart, inTheColumn,
                                                  ClearInfo.ClearDirection.Column));
                }
            }
        }

        /// <summary>
        /// Applies gravity.  This is automatically called with TryClear().
        /// </summary>
        private bool ApplyGravity()
        {
            bool tryClearAgain = false;
            for (int row = 1; row < Rows; row++) {
                for (int column = 0; column < Columns; column++) {
                    GameBlockState thisState = this[column, row];
                    if (thisState.Empty) {
                        continue;
                    } else if (thisState.Hard) {
                        ApplyGravityOnHardRow(row);
                        continue;
                    }
                    GameBlockState lowestState = null;
                    for (int lowerRow = row - 1; lowerRow > 0; lowerRow--) {
                        GameBlockState otherState = this[column, lowerRow];
                        if (!otherState.Empty)
                            break;
                        lowestState = otherState;
                    }
                    if (lowestState != null) {
                        thisState.MoveDown(lowestState);
                        tryClearAgain = true;
                    }
                }
            }
            return tryClearAgain;
        }

        /// <summary>
        /// Applies gravity on hard rows.  Hard blocks always have to be rows
        /// so when the first hard block is found this is called instead of
        /// the regular gravity logic for the whole row.
        /// </summary>
        private void ApplyGravityOnHardRow(int row)
        {
            int column;
            int rowToFallDownTo = -1;
            for (int probeRow = row - 1; probeRow > 1; probeRow--) {
                for (column = 0; column < Columns; column++) {
                    if (!this[column, probeRow].Empty)
                        break;
                }
                if (column != Columns)
                    break;
                rowToFallDownTo = probeRow;
            }

            if (rowToFallDownTo >= 0)
                for (column = 0; column < Columns; column++)
                    this[column, row].MoveDown(this[column, rowToFallDownTo]);
        }

        /// <summary>
        /// Helper function that checks if the game over condition was triggered.
        /// Sets the gameOver flag, explodes blocks and calls into OnGameOver().
        /// </summary>
        private void CheckForGameOver()
        {
            for (int column = 0; column < Columns; column++) {
                if (!this[column, Rows - 1].Empty) {
                    gameOver = true;
                    ExplodeAllBlocks();
                    OnGameOver();
                }
            }
        }

        /// <summary>
        /// Replaces all blocks with animations of exploding blocks.
        /// </summary>
        private void ExplodeAllBlocks()
        {
            for (int row = 0; row < Rows; row++)
                for (int column = 0; column < Columns; column++)
                    this[column, row].Explode();
        }

        /// <summary>
        /// Callback for cleared blocks that notifies the session.
        /// </summary>
        public void OnBlocksCleared(int blockCount, int lineCount)
        {
            if (Session.Running)
                Session.OnBlocksCleared(this, blockCount, lineCount);
        }

        /// <summary>
        /// Callback for game over situation that notifies the session.
        /// </summary>
        public void OnGameOver()
        {
            if (Session.Running)
                Session.OnGameOver(this);
        }

        public void Update(FrameEventArgs args)
        {
            jitter = (jitter + (float)args.Time * 7.0f) % 360.0f;

            if (!gameOver) {
                scrollY += (float)args.Time * (fastScrollMode ? 150.0f : scrollSpeed);
                if (scrollY >= BlockSize) {
                    scrollY -= BlockSize;
                    InsertNewRow(false);
                    MoveCursorUp();
                }
            }

            if (Math.Sin(jitter * 50) > 0.5f)
                cursorTexture = regularBlocks[rnd.Next() % regularBlocks.Length].Texture;

            HashSet<GameBlockAnimation> toDelete = new HashSet<GameBlockAnimation>();
            foreach (GameBlockAnimation blockAnimation in activeAnimations)
                if (!blockAnimation.Update(args))
                    toDelete.Add(blockAnimation);
            foreach (GameBlockAnimation blockAnimation in toDelete)
                activeAnimations.Remove(blockAnimation);

            for (int column = 0; column < Columns; column++)
                for (int row = 0; row < Rows; row++)
                    this[column, row].UpdateAnimation(args);
        }

        public void Draw(DrawContext ctx)
        {
            List<GameBlockState> deferredBlocks = new List<GameBlockState>();
            float scale = 1.0f + (float)Math.Sin(jitter * 0.5f) * 0.003f;
            float angle = (float)Math.Sin(jitter * 0.5) * 0.15f;

            ctx.Push();
            ctx.Translate(position.X, position.Y);
            ctx.ScaleAroundPoint(scale, scale, Columns * BlockSize / 2, Rows * BlockSize / 2);
            ctx.RotateAroundPoint(angle, Columns * BlockSize / 3, Rows * BlockSize / 3);

            // cursor
            if (!gameOver) {
                ctx.Push();
                ctx.Translate(GetBlockX(cursorColumn) - 4, GetBlockY(cursorRow) - 4);
                ctx.BindTexture(cursorTexture);
                ctx.SetColor(new Color4(100, 100, 100, 255));
                ctx.DrawTexturedRectangle(BlockSize * 2.0f + 8, BlockSize + 8, cursorTexture);
                ctx.Pop();
            // game over logo
            } else {
                ctx.Push();
                ctx.Translate(40.0f, 240.0f);
                ctx.BindTexture(session.GameOverTexture);
                ctx.DrawTexturedRectangle(240.0f, 45.0f, session.GameOverTexture);
                ctx.Pop();
            }

            // regular blocks
            for (int column = 0; column < Columns; column++) {
                for (int row = 0; row < Rows; row++) {
                    GameBlockState blockState = this[column, row];
                    if (blockState.Empty)
                        continue;
                    if (blockState.DrawDeferred)
                        deferredBlocks.Add(blockState);
                    else
                        blockState.Draw(ctx);
                }
            }

            // block independent block animations
            foreach (GameBlockAnimation animation in activeAnimations)
                animation.Draw(ctx);

            // top and bottom bars that hide partial rows
            if (!gameOver) {
                ctx.Push();
                ctx.BindTexture(session.BarTexture);
                ctx.SetColor(new Color4(40, 40, 40, 255));
                ctx.Translate(-BlockSize / 2, -35.0f);
                ctx.DrawTexturedRectangle(BlockSize * (Columns + 1), 40.0f, session.BarTexture);
                ctx.Translate(0.0f, BlockSize * Rows - BlockSize / 2);
                ctx.DrawTexturedRectangle(BlockSize * (Columns + 1), 50.0f, session.BarTexture);
                ctx.Pop();
            }

            // selected blocks and other things we want to have on top
            foreach (GameBlockState blockState in deferredBlocks)
                blockState.Draw(ctx);

            ctx.Pop();
        }

        public void MoveCursorUp()
        {
            if (cursorRow < Rows - 1)
                cursorRow++;
        }

        public void MoveCursorDown()
        {
            if (cursorRow > 1)
                cursorRow--;
        }

        public void MoveCursorLeft()
        {
            if (cursorColumn > 0)
                cursorColumn--;
        }

        public void MoveCursorRight()
        {
            if (cursorColumn < Columns - 2)
                cursorColumn++;
        }

        /// <summary>
        /// Exchanges the two blocks under the cursor.
        /// </summary>
        public void PerformExchange()
        {
            GameBlockState blockState = this[cursorColumn, cursorRow];
            if (blockState.Hard)
                return;
            blockState.ExchangeWith(this[cursorColumn + 1, cursorRow]);
            TryClear();
        }

        /// <summary>
        /// Registers a new block animation for this pane.
        /// </summary>
        public void AddAnimation(GameBlockAnimation animation)
        {
            activeAnimations.Add(animation);
        }

        public void IncreaseSpeed()
        {
            float newSpeed = scrollSpeed + SpeedChangeValue;
            scrollSpeed = Math.Min(MaximumScrollSpeed, newSpeed);
        }

        public void DecreaseSpeed()
        {
            float newSpeed = scrollSpeed - SpeedChangeValue;
            scrollSpeed = Math.Max(MinimumScrollSpeed, newSpeed);
        }

        public float GetBlockX(int column) {
            return column * BlockSize;
        }

        public float GetBlockY(int row) {
            return (Rows - row - 1) * BlockSize - ScrollY;
        }

        public GameSession Session {
            get { return session; }
        }
        public GameBlockState this[int column, int row] {
            get { return grid[column, row]; }
            set { grid[column, row] = value; }
        }
        public float Jitter {
            get { return jitter; }
        }
        public Vector2 Position {
            get { return position; }
        }
        public int CursorColumn {
            get { return cursorColumn; }
        }
        public int CursorRow {
            get { return cursorRow; }
        }
        public float BlockSize {
            get { return 32.0f; }
        }
        public int Columns {
            get { return 10; }
        }
        public int Rows {
            get { return 18; }
        }
        public float ScrollSpeed {
            get { return scrollSpeed; }
            set { scrollSpeed = value; }
        }
        public float DefaultScrollSpeed {
            get { return 6.0f; }
        }
        public float MinimumScrollSpeed {
            get { return 4.0f; }
        }
        public float MaximumScrollSpeed {
            get { return 20.0f; }
        }
        public float SpeedChangeValue {
            get { return 4.0f; }
        }
        public bool FastScrollMode {
            get { return fastScrollMode; }
            set { fastScrollMode = value; }
        }
        public float ScrollY {
            get { return scrollY; }
        }
        public bool GameOver {
            get { return gameOver; }
        }
    }
}

