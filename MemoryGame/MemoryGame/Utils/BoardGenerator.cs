// Utils/BoardGenerator.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemoryGame.Utils
{
    public static class BoardGenerator
    {
        private static Random rand = new Random();

        public static string[,] GenerateBoard(string[] uniqueSymbols, int rows, int cols)
        {
            string[,] board = new string[rows, cols];
            List<string> pool = new List<string>();
            
            // Create 2 of each symbol for the pairs
            foreach (var sym in uniqueSymbols)
            {
                pool.Add(sym);
                pool.Add(sym);
            }

            // Execute the backtracking algorithm to populate the board safely
            Solve(board, pool, 0, rows, cols);
            return board;
        }

        private static bool Solve(string[,] board, List<string> pool, int index, int rows, int cols)
        {
            // If we filled the board, we are done!
            if (index == rows * cols) return true;

            int r = index / cols;
            int c = index % cols;

            // Shuffle distinct available symbols to ensure layout is truly random every time
            var availableSymbols = pool.Distinct().OrderBy(x => rand.Next()).ToList();

            foreach (var symbol in availableSymbols)
            {
                if (IsValid(board, r, c, symbol, rows, cols))
                {
                    board[r, c] = symbol; // Place the symbol
                    pool.Remove(symbol);  // Remove one instance from the pool

                    if (Solve(board, pool, index + 1, rows, cols))
                        return true;

                    // Backtrack if this placement leads to an unsolvable board later
                    pool.Add(symbol);
                    board[r, c] = null;
                }
            }
            return false;
        }

        private static bool IsValid(string[,] board, int r, int c, string symbol, int rows, int cols)
        {
            // Check all 8 neighboring directions (Horizontal, Vertical, Diagonal)
            int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

            for (int i = 0; i < 8; i++)
            {
                int nr = r + dr[i];
                int nc = c + dc[i];

                if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                {
                    // If a neighbor is the exact same symbol, placement is invalid
                    if (board[nr, nc] == symbol) return false;
                }
            }
            return true;
        }
    }
}