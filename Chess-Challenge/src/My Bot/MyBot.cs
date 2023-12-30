using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static MyBot;


public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    { 
        int maxDepth = 3;
        return minimax(board, maxDepth, true, board.IsWhiteToMove).Item2;

    }

    public int evaluate(Board board, bool maximizingColor)
    {
        int[] values = { 0, 10, 30, 30, 50, 90, 900 };
        int eval = 0;
        if(maximizingColor)
        {
            for (int i = 1; i <= 6; i++)
            {
                IEnumerator<Piece> enumerator = board.GetPieceList((PieceType)i, maximizingColor).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    eval += values[(int)enumerator.Current.PieceType];
                }
                enumerator = board.GetPieceList((PieceType)i, !maximizingColor).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    eval -= values[(int)enumerator.Current.PieceType];
                }
            }
            return eval;
        }
        return -eval;
    }

    public (int, Move) minimax(Board board, int depth, bool maximizingPlayer, bool maximizingColor)
    {
        Move bestMove;
        int value;
        Move[] moves = board.GetLegalMoves();
        if (depth == 0 && board.GetLegalMoves().Length > 0)
        {
            return (evaluate(board, maximizingColor), moves[0]);
        }
        else if (!board.IsInStalemate() && !board.IsDraw() && !board.IsInCheckmate())
        {
            if (maximizingPlayer)
            {
                value = -int.MaxValue;
                bestMove = moves[0];
                for(int i = 0; i < board.GetLegalMoves().Length; i++)
                {
                    Move currMove = moves[i];
                    board.MakeMove(currMove);
                    (int, Move) current = minimax(board, depth - 1, false, maximizingColor);
                    board.UndoMove(currMove);
                    if (current.Item1 > value)
                    {
                        value = current.Item1;
                        bestMove = currMove;
                    }
                }
                return (value, bestMove);
            }
            else
            {
                value = int.MaxValue;
                bestMove = moves[0];
                for (int i = 0; i < moves.Length; i++)
                {
                    Move currMove = moves[i];
                    board.MakeMove(currMove);
                    (int, Move) current = minimax(board, depth - 1, true, maximizingColor);
                    board.UndoMove(currMove);
                    if (current.Item1 < value)
                    {
                        value = current.Item1;
                        bestMove = currMove;
                    }
                }
                return (value, bestMove);
            }
        }
        else
        {
            return (0, Move.NullMove);
        }
    }
}