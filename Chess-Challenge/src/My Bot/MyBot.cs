using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static MyBot;


public class MyBot : IChessBot
{
    int maxDepth = 6;
    int[] values = { 0, 10, 30, 30, 50, 90, 900 };

    public Move Think(Board board, Timer timer)
    {
        (int, Move) result = minimax(board, maxDepth, board.IsWhiteToMove, board.IsWhiteToMove, -int.MaxValue, int.MaxValue);
        return result.Item2;
    }

    public int evaluate(Board board, bool maximizingColor, bool maximizingPlayer)
    {
        int myScore = 0;
        int opScore = 0;
        int eval = 0;

        for (int i = 1; i <= 6; i++)
        {
            IEnumerator<Piece> enumerator = board.GetPieceList((PieceType)i, maximizingPlayer).GetEnumerator();
            while (enumerator.MoveNext())
            {
                myScore += values[(int)enumerator.Current.PieceType];
            }
            enumerator = board.GetPieceList((PieceType)i, !maximizingPlayer).GetEnumerator();
            while (enumerator.MoveNext())
            {
                opScore += values[(int)enumerator.Current.PieceType];
            }
        }

        // Encourage Passed Pawns
        IEnumerator<Piece> enumeratorPP = board.GetPieceList(PieceType.Pawn, maximizingPlayer).GetEnumerator();
        while (enumeratorPP.MoveNext())
        {
            if (board.IsWhiteToMove == maximizingPlayer)
            {
                myScore += (int)(Math.Exp(enumeratorPP.Current.Square.Rank) / 32f);
            }
            else
            {
                myScore += (int)(Math.Exp(7 - enumeratorPP.Current.Square.Rank) / 32f);
            }
        }
        enumeratorPP = board.GetPieceList(PieceType.Pawn, !maximizingPlayer).GetEnumerator();
        while (enumeratorPP.MoveNext())
        {
            if (board.IsWhiteToMove == !maximizingPlayer)
            {
                opScore += (int)(Math.Exp(7 - enumeratorPP.Current.Square.Rank) / 22f);
            }
            else
            {
                opScore += (int)(Math.Exp(enumeratorPP.Current.Square.Rank) / 22f);
            }
        }

        eval = myScore - opScore;

        if (board.IsInCheckmate() && (board.IsWhiteToMove == maximizingPlayer))
        {
            eval = -5000;
        }
        else if (board.IsInCheckmate() && (board.IsWhiteToMove != maximizingPlayer))
        {
            eval = 5000;
        }
        else if (board.IsDraw() && eval < 0 && (board.IsWhiteToMove == maximizingPlayer))
        {
            eval = 3000;
        }
        else if (board.IsDraw() && eval > 0 && (board.IsWhiteToMove != maximizingPlayer))
        {
            eval = -3000;
        }
        else if (board.IsRepeatedPosition() && eval < 0 && (board.IsWhiteToMove == maximizingPlayer))
        {
            eval = 3000;
        }
        else if (board.IsRepeatedPosition() && eval > 0 && (board.IsWhiteToMove != maximizingPlayer))
        {
            eval = -3000;
        }
        if (maximizingPlayer)
        {
            return eval;
        }
        else
        {
            return -eval;
        }
    }

    public (int, Move) minimax(Board board, int depth, bool maximizingPlayer, bool maximizingColor, int alpha, int beta)
    {
        Move bestMove;
        int value;
        Move[] moves = board.GetLegalMoves();
        Move[] movesOrdered = new Move[moves.Length];
        // Move Ordering (MVV-LVA)
        int k = 0;
        bool capturesExist = false;
        for (int l = 0; l < moves.Length; l++)
        {
            if (moves[l].IsCapture)
            {
                capturesExist = true;
            }
        }
        if (capturesExist)
        {
            for (int l = 0; l < moves.Length; l++)
            {
                for (int i = 5; i >= 1; i--)
                {
                    for (int j = 1; j <= 5; j++)
                    {
                        if (moves[l].MovePieceType == (PieceType)j && moves[l].CapturePieceType == (PieceType)i)
                        {
                            movesOrdered[k] = moves[l];
                            moves[l] = Move.NullMove;
                            k++;
                        }
                    }
                }
            }
            for (int l = 0; l < moves.Length; l++)
            {
                if (moves[l] != Move.NullMove)
                {
                    movesOrdered[k] = moves[l];
                    k++;
                }
            }
            moves = movesOrdered;
        }
        
        
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            if (moves.Length > 0)
            {
                return (evaluate(board, maximizingColor, maximizingPlayer), moves[0]);
            }
            return (evaluate(board, maximizingColor, maximizingPlayer), Move.NullMove);
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
                    (int, Move) current = minimax(board, depth - 1, false, maximizingColor, alpha, beta);
                    if(board.IsInCheckmate())
                    {
                        current.Item1 = current.Item1 * depth;
                    }
                    board.UndoMove(currMove);
                    if (current.Item1 > value)
                    {
                        value = current.Item1;
                        bestMove = currMove;
                    }
                    alpha = Math.Max(alpha, current.Item1);
                    if (beta <= alpha) break;
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
                    (int, Move) current = minimax(board, depth - 1, true, maximizingColor, alpha, beta);
                    if (board.IsInCheckmate())
                    {
                        current.Item1 = current.Item1 * depth;
                    }
                    board.UndoMove(currMove);
                    if (current.Item1 < value)
                    {
                        value = current.Item1;
                        bestMove = currMove;
                    }
                    beta = Math.Min(beta, current.Item1);
                    if (beta <= alpha) break;
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