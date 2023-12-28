using ChessChallenge.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;






public class MyBot : IChessBot
{

	public int maxSearchTime = 0;
	public bool isWhite;
	public Board board;
	public Timer timer;


	public Move Think(Board board, Timer timer) {

	
		// FOR TESTING ONLY
		
		
		Board b = Board.CreateBoardFromFEN("r3k3/8/8/8/8/8/5PPP/6KR w KAhq - 0 1");  // # DEBUG
		Console.Write(b.CreateDiagram()); // # DEBUG
		(int e, Move m) = this.NegaMax(b, 3, int.MinValue, int.MaxValue, false); // # DEBUG
		Console.WriteLine("Eval: " + e.ToString() + " " + m.ToString()); // # DEBUG
		Console.WriteLine("Shallow Eval: " + this.Eval(b).ToString()); // # DEBUG
		b.MakeMove(m); // # DEBUG
		(e, m) = this.NegaMax(b, 3, int.MinValue+1, int.MaxValue-1, false); // # DEBUG
		Console.WriteLine("Eval: " + e.ToString() + " " + m.ToString()); // # DEBUG
		Console.WriteLine("Shallow Eval: " + this.Eval(b).ToString()); // # DEBUG

		// END TESTING	
		this.isWhite = board.IsWhiteToMove;
		this.timer = timer;

		(int bestEval, Move best) = this.NegaMax(board, 4, int.MinValue+1, int.MaxValue-1, false); // Run a search for the best move

		// Once as I was letting it play games against EvilBot it returned a null move, I have not been able to replicate so I've thrown in this debug shit too catch it if it repeats
		if (best == Move.NullMove) {  // #DEBUG
			Console.WriteLine(board.CreateDiagram()); // #DEBUG
			Console.WriteLine(bestEval); // #DEBUG
			throw new Exception("Null Move"); // #DEBUG
		}// #DEBUG		
		

		Console.WriteLine("Best Move: " + best.ToString() + " Eval: " + bestEval.ToString()); // #DEBUG
		return best; 
	}

	// My attempt at a NegaMax implementation, might still have a few bugs that need to be ironed out.
	public (int, Move) NegaMax(Board board, int depth, int alpha, int beta,  bool quiescence) {
		// this is easier than passing in color every time
		int color = board.IsWhiteToMove ? 1 : -1;
	
		Move[] moves = board.GetLegalMoves(quiescence);
	
		if (depth <= 0 || moves.Length == 0) return (color*this.Eval(board), Move.NullMove);
		// For alpha-beta pruning too work optimally we need to sort the moves so that better moves are more likely too come earlier
		// This means we can eliminate more nodes using alpha-beta pruning.
 
		// Compares the order in which we should approach two moves.
		int compare(Move move1, Move move2) {
			if (move1.Equals(move2)) {
				return 0; // same move so order doesn't matter,  probably not needed
			}

			List<int> pieceVals = new List<int>(6) {0, 100, 300, 340, 500, 900, 0}; // An array storing the different values of pieces where the index is the PieceType 

			int getValue(Move move) {
				if (move.IsCapture) {
					return pieceVals[(int) move.CapturePieceType] - (int) (pieceVals[(int) move.MovePieceType]*0.5);
				} else if (move.IsPromotion) {
					return pieceVals[(int) move.PromotionPieceType];
				}
				return 0;
			}

			return getValue(move2) - getValue(move1);
		}
		
		Array.Sort(moves, compare);	
		Move bestMove = Move.NullMove;
		int eval = int.MinValue;
	
		foreach (Move move in moves) {
			
			// Some more speculation based on the entry in line 40, I could be going insane but I'm leaving this here out of superstition
			if (move == Move.NullMove) { // #DEBUG
				Console.WriteLine(board.CreateDiagram()); // #DEBUG
				throw new Exception("Null Move"); // #DEBUG
			} // #DEBUG

			board.MakeMove(move);

			int depthChange = 1;
			if (board.SquareIsAttackedByOpponent(board.GetKingSquare(board.IsWhiteToMove))) {
				depthChange = 0;
			}

			int newEval = -this.NegaMax(board, depth -depthChange,  -beta, -alpha, quiescence).Item1;

			board.UndoMove(move);

			if (newEval >= beta) {
				eval = newEval;
				bestMove = move;
				break;
			}
		
			if (newEval > eval) {
				eval = newEval;
				bestMove = move;
				alpha = Math.Max(alpha, eval);
			}

		}
	

		return (eval, bestMove);

	}

	public int Eval(Board board) {
		if (board.IsDraw()) {
			return board.IsWhiteToMove ? -1 : 1;
		}
		
		if (board.IsInCheckmate()) {
			if (board.IsWhiteToMove)
				return int.MinValue+1;
			else	
				return int.MaxValue-1;
		}



		if (board.GetLegalMoves(true).Length != 0) return this.NegaMax(board, int.MaxValue, int.MinValue, int.MaxValue,  true).Item1;

		List<int> pieceVals = new List<int>(6) {100, 300, 340, 500, 900, 0};

		int eval = 0;
		// Console.WriteLine(pieceVals[(int) PieceType.Queen - 1].ToString());
		for (int i = 0; i<pieceVals.Count; i++) {
			eval += board.GetPieceList((PieceType) (i+1), true).Count * pieceVals[i] ;
			
			eval -= board.GetPieceList((PieceType) (i+1), false).Count * pieceVals[i] ;
		}
		return eval;	
		if (board.IsWhiteToMove) {
			eval += board.GetLegalMoves().Length;
			eval -= BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(PieceType.Queen, board.GetKingSquare(true), board.AllPiecesBitboard, true));
			board.ForceSkipTurn();
			eval -= board.GetLegalMoves().Length;
			board.UndoSkipTurn();
		} else {
			eval -= board.GetLegalMoves().Length;
			eval += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(PieceType.Queen, board.GetKingSquare(false), board.AllPiecesBitboard, false));
			board.ForceSkipTurn();
			eval += board.GetLegalMoves().Length;
			board.UndoSkipTurn();
		}

		return eval;
	}

	
}
