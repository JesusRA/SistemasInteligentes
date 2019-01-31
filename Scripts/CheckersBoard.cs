using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckersBoard : MonoBehaviour {

    public class Move
    {
        public Vector2Int inicio;
        public Vector2Int destino;

        public Move ()
        {
            this.inicio = new Vector2Int();
            this.destino = new Vector2Int();
        }

        public Move (int xInicial, int yInicial, int xDestino, int yDestino)
        {
            this.inicio = new Vector2Int(xInicial, yInicial);
            this.destino = new Vector2Int(xDestino, yDestino);
        }
    }

    public Piece[,] pieces = new Piece[8, 8];
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;

    private Piece selectedPiece;
    private List<Piece> forcedPieces;

    private Vector3 boardOffset = new Vector3(-4.0f, 0, -4.0f);
    private Vector3 pieceOffset = new Vector3(0.5f, 0, 0.5f);
        
    public bool isWhite;
    public int depth;

    private bool isWhiteTurn;
    private bool hasKilled;

    private Vector2 mouseOver;
    private Vector2 startDrag;
    private Vector2 endDrag;

    private void Start()
    {
        forcedPieces = new List<Piece>();
        isWhiteTurn = true;
        isWhite = true;        
        GenerateBoard();
    }

    private void Update()
    {
        UpdateMouseOver();        

        if ((isWhite)?isWhiteTurn:!isWhiteTurn)
        {            
            if (GameController.PvsIA)
            {
                if (isWhiteTurn)
                {
                    int x = (int)mouseOver.x;
                    int y = (int)mouseOver.y;

                    if (selectedPiece != null)
                        UpdatePieceDrag(selectedPiece);
                    if (Input.GetMouseButtonDown(0))
                        SelectPiece(x, y);

                    if (Input.GetMouseButtonUp(0))
                        TryMove((int)startDrag.x, (int)startDrag.y, x, y);
                }
                else
                {
                    bool whiteSide = isWhiteTurn;
                    bool maximizingPlayer = true;
                    Move movimiento = new Move();
                    movimiento = MinimaxStart(pieces, depth, whiteSide, maximizingPlayer);
                    int xStart = movimiento.inicio.x;
                    int yStart = movimiento.inicio.y;
                    int x = movimiento.destino.x;
                    int y = movimiento.destino.y;
                    TryMove(xStart, yStart, x, y);
                }
            }
            else
            {
                int x = (int)mouseOver.x;
                int y = (int)mouseOver.y;

                if (selectedPiece != null)
                    UpdatePieceDrag(selectedPiece);
                if (Input.GetMouseButtonDown(0))
                    SelectPiece(x, y);

                if (Input.GetMouseButtonUp(0))
                    TryMove((int)startDrag.x, (int)startDrag.y, x, y);
            }
        }
    }

    private void GenerateBoard()
    {
        // White team
        for (int y = 0; y < 3; y++)
        {
            bool oddRow = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2)
            {
                GeneratePiece((oddRow) ? x : x + 1, y);
            }
        }
        // Black team
        for (int y = 7; y > 4; y--)
        {
            bool oddRow = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2)
            {
                GeneratePiece((oddRow) ? x : x + 1, y);
            }
        }
    }

    private void GeneratePiece(int x, int y)
    {
        bool isWhite = (y > 3) ? false : true;
        GameObject go = Instantiate((isWhite) ? whitePiecePrefab : blackPiecePrefab) as GameObject;
        go.transform.SetParent(transform);
        Piece piece = go.GetComponent<Piece>();
        pieces[x, y] = piece;
        MovePiece(piece, x, y);
    }

    private void MovePiece(Piece piece, int x, int y)
    {
        piece.transform.position = (Vector3.right * x) + (Vector3.forward * y) + boardOffset + pieceOffset;              
    }

    private void UpdateMouseOver()
    {
        if (!Camera.main)
        {
            Debug.Log("Unable to find main camera");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            mouseOver.x = (int)(hit.point.x - boardOffset.x);
            mouseOver.y = (int)(hit.point.z - boardOffset.z);
        }
        else
        {
            mouseOver.x = -1;
            mouseOver.y = -1;
        }
    }

    private void UpdatePieceDrag(Piece p)
    {
        if (!Camera.main)
        {
            Debug.Log("Unable to find main camera");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            p.transform.position = hit.point + Vector3.up;
        }
    }

    private void SelectPiece(int x, int y)
    {
        // Fuera del tablero
        if (x < 0 || x >= 8 || y < 0 || y >= 8)
            return;

        Piece piece = pieces[x, y];
        if (piece != null && piece.isWhite == isWhite)
        {
            if (forcedPieces.Count == 0)
            {
                selectedPiece = piece;
                startDrag = mouseOver;
            }
            else
            {
                //Look for the piece under our forced list
                if (forcedPieces.Find(fp => fp == piece) == null)
                    return;

                selectedPiece = piece;
                startDrag = mouseOver;
            }
        }
    }

    private void TryMove(int xStart, int yStart, int x, int y)
    {
        forcedPieces = ScanForPossibleMove();
        // Multiplayer
        startDrag = new Vector2(xStart, yStart);
        endDrag = new Vector2(x, y);
        selectedPiece = pieces[xStart, yStart];

        //Out of boounds
        if (x < 0 || x >= 8 || y < 0 || y >= 8)
        {
            if (selectedPiece != null)
                MovePiece(selectedPiece, xStart, yStart);

            startDrag = Vector2.zero;
            selectedPiece = null;
            return;
        }

        if (selectedPiece != null)
        {
            //If it has not moved
            if (endDrag == startDrag)
            {
                MovePiece(selectedPiece, xStart, yStart);
                startDrag = Vector2.zero;
                selectedPiece = null;
                return;
            }

            //Check if valid move
            if (selectedPiece.ValidMove(pieces, xStart, yStart, x, y))
            {
                //Did we kill anythin 
                //If this is a jump
                if (Mathf.Abs(xStart - x) == 2)
                {
                    Piece p = pieces[(xStart + x) / 2, (yStart + y) / 2];
                    if (p != null)
                    {
                        pieces[(xStart + x) / 2, (yStart + y) / 2] = null;
                        Destroy(p.gameObject);
                        hasKilled = true;
                    }
                }

                //Were we supposed to kill anything
                if (forcedPieces.Count != 0 && !hasKilled)
                {
                    MovePiece(selectedPiece, xStart, yStart);
                    startDrag = Vector2.zero;
                    selectedPiece = null;
                    return;
                }

                pieces[x, y] = selectedPiece;
                pieces[xStart, yStart] = null;
                MovePiece(selectedPiece, x, y);

                EndTurn();
            }
            else
            {
                MovePiece(selectedPiece, xStart, yStart);
                startDrag = Vector2.zero;
                selectedPiece = null;
                return;
            }
        }       
    }

    private void EndTurn()
    {
        int x = (int)endDrag.x;
        int y = (int)endDrag.y;

        //Promotions
        if(selectedPiece != null)
        {
            if (selectedPiece.isWhite && !selectedPiece.isKing && y == 7)
            {
                selectedPiece.isKing = true;
                selectedPiece.transform.Rotate(Vector3.right * 180);
            }
            else if (!selectedPiece.isWhite && !selectedPiece.isKing && y == 0)
            {
                selectedPiece.isKing = true;
                selectedPiece.transform.Rotate(Vector3.right * 180);
            }
        }

        selectedPiece = null;
        startDrag = Vector2.zero;

        if (ScanForPossibleMove(selectedPiece, x, y).Count != 0 && hasKilled)
            return;

        isWhiteTurn = !isWhiteTurn;
        isWhite = !isWhite;
        hasKilled = false;
        CheckVictory();
    }

    private void CheckVictory()
    {
        int whites = 0;
        int blacks = 0;
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (pieces[i, j] != null && pieces[i, j].isWhite)
                    whites++;
                else if (pieces[i, j] != null && !pieces[i, j].isWhite)
                    blacks++;
            }

        if (whites == 0)
            Victory(false);
        if (blacks == 0)
            Victory(true);
    }

    private void Victory(bool isWhite)
    {
        if (isWhite)
            GameController.whiteWon = true;      
        else
            GameController.whiteWon = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private List<Piece> ScanForPossibleMove(Piece p, int x, int y)
    {
        forcedPieces = new List<Piece>();

        if (pieces[x, y].IsForceToMove(pieces,x,y))
            forcedPieces.Add(pieces[x, y]);
     
        return forcedPieces;
    }

    private List<Piece> ScanForPossibleMove()
    {
        forcedPieces = new List<Piece>();

        //Check all the pieces
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if (pieces[i, j] != null && pieces[i, j].isWhite == isWhiteTurn)
                    if (pieces[i, j].IsForceToMove(pieces, i, j))
                        forcedPieces.Add(pieces[i, j]);

        return forcedPieces;
    }

    private Move MinimaxStart (Piece[,] board, int depth, bool whiteSide, bool maximizingPlayer)
    {
        double alpha = double.NegativeInfinity;
        double beta = double.PositiveInfinity;

        List<Move> possibleMoves = new List<Move>();
        List<Vector2> forcedPieces = new List<Vector2>();

        // Mirar si es obligatorio matar con alguna pieza y guardar sus x e y
        forcedPieces = FindForcedPieces(board, whiteSide);
        
        // Si hay alguna incluir los movimientos posibles de ellas
        if (forcedPieces.Count > 0)
        {
            possibleMoves = GetForcedMoves(board, forcedPieces);
        }

        // Si no incluir todos los movimientos posibles
        else if (forcedPieces.Count == 0)
        {            
            possibleMoves = GetAllValidMoves(board, whiteSide);
        }

        // Si no hay movimientos posibles return null
        if (possibleMoves.Count == 0)
        {
            Debug.Log("NO HAY MOVIMIENTOS");
            return null;
        }            

        // Crear la lista para guardar los valores de los estados
        List<double> heuristics = new List<double>();

        // Crear un tablero temporal
        Piece[,] tempBoard;

        // Por cada uno de los movimientos posibles
        for (int i = 0; i < possibleMoves.Count; i++)
        {
            tempBoard = (Piece[,])board.Clone();
            // Hacer el movimiento en el tablero temporal
            MakeMove(tempBoard, possibleMoves[i]);
            // Hacer el calculo
            heuristics.Add(Minimax(tempBoard, depth - 1, !whiteSide, !maximizingPlayer, alpha, beta));
        }

        // Buscar el valor maximo en la lista de heuristicas
        double maxHeuristics = double.NegativeInfinity;
        for (int i = heuristics.Count - 1; i >= 0; i--)
        {
            if (heuristics[i] >= maxHeuristics)
            {
                maxHeuristics = heuristics[i];
            }
        }

        // Filtrar segun resultado dejando solo los movimientos mas valiosos
        for (int i = 0; i < heuristics.Count; i++)
        {
            if (heuristics[i] < maxHeuristics)
            {
                heuristics.RemoveAt(i);
                possibleMoves.RemoveAt(i);
                i--;
            }
        }

        // Devolver aleatoriamente un movimiento de entre los de igual valia
        int rand = (int)Random.Range(0, possibleMoves.Count);
        return possibleMoves[rand];      
    }

    private List<Vector2> FindForcedPieces (Piece[,] board, bool whiteSide)
    {
        List<Vector2> forcedPieces = new List<Vector2>();
        
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (board[i, j] != null)
                {
                    Piece p = board[i, j];
                    if (whiteSide)
                    {
                        if (p.isWhite)
                            if (p.IsForceToMove(board, i, j))
                                forcedPieces.Add(new Vector2(i, j));                                
                    }
                    else
                    {
                        if (!p.isWhite)
                            if (p.IsForceToMove(board, i, j))
                                forcedPieces.Add(new Vector2(i, j));
                    }
                }                
            }
        return forcedPieces;
    }

    private List<Move> GetForcedMoves(Piece[,] board, List<Vector2> forcedPieces)
    {
        List<Move> forcedMoves = new List<Move>();

        foreach (Vector2 pos in forcedPieces)
        {
            int x = (int)pos.x;
            int y = (int)pos.y;
            Piece piece = board[x, y];

            if (piece.isWhite || piece.isKing)
            {
                //Top left
                if (x >= 2 && y <= 5)
                {
                    Piece p = board[x - 1, y + 1];
                    //If there is a piece, and its not the same color as ours
                    if (p != null && p.isWhite != piece.isWhite)
                    {
                        Move mov = new Move(x, y, x - 2, y + 2);
                        forcedMoves.Add(mov);
                    }
                }
                //Top right
                if (x <= 5 && y <= 5)
                {
                    Piece p = board[x + 1, y + 1];
                    //If there is a piece, and its not the same color as ours
                    if (p != null && p.isWhite != piece.isWhite)
                    {
                        Move mov = new Move(x, y, x + 2, y + 2);
                        forcedMoves.Add(mov);
                    }
                }
            }
            if (!piece.isWhite || piece.isKing)
            {
                //Bottom left
                if (x >= 2 && y >= 2)
                {
                    Piece p = board[x - 1, y - 1];
                    //If there is a piece, and its not the same color as ours
                    if (p != null && p.isWhite != piece.isWhite)
                    {
                        Move mov = new Move(x, y, x - 2, y - 2);
                        forcedMoves.Add(mov);
                    }
                }
                //Bottom right
                if (x <= 5 && y >= 2)
                {
                    Piece p = board[x + 1, y - 1];
                    //If there is a piece, and its not the same color as ours
                    if (p != null && p.isWhite != piece.isWhite)
                    {
                        Move mov = new Move(x, y, x + 2, y - 2);
                        forcedMoves.Add(mov);
                    }
                }
            }
        }

        return forcedMoves;
    }

    private List<Move> GetAllValidMoves (Piece[,] board, bool whiteSide)
    {
        List<Move> allValidMoves = new List<Move>();

        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (board[i, j] != null)
                {
                    Piece piece = board[i, j];
                    int xStart = i;
                    int yStart = j;

                    if (whiteSide)
                    {
                        if (piece.isWhite)
                        {
                            //Top left    
                            int x = xStart - 1;
                            int y = yStart + 1;
                            if (xStart >= 1 && yStart <= 6)
                            {                 
                                if (piece.ValidMove(board, xStart, yStart, x, y))
                                {
                                    Move mov = new Move(xStart, yStart, x, y);
                                    allValidMoves.Add(mov);
                                }
                            }

                            //Top right                                                       
                            if (xStart <= 6 && yStart <= 6)
                            {
                                x = xStart + 1;
                                y = yStart + 1;
                                if (piece.ValidMove(board, xStart, yStart, x, y))
                                {
                                    Move mov = new Move(xStart, yStart, x, y);
                                    allValidMoves.Add(mov);
                                }
                            }

                            //If king
                            if (piece.isKing)
                            {
                                //Bottom left
                                if (xStart >= 1 && yStart >= 1)
                                {
                                    x = xStart - 1;
                                    y = yStart - 1;
                                    if (piece.ValidMove(board, xStart, yStart, x, y))
                                    {
                                        Move mov = new Move(xStart, yStart, x, y);
                                        allValidMoves.Add(mov);
                                    }
                                }

                                //Bottom right
                                if (xStart <= 6 && yStart >= 1)
                                {
                                    x = xStart + 1;
                                    y = yStart - 1;
                                    if (piece.ValidMove(board, xStart, yStart, x, y))
                                    {
                                        Move mov = new Move(xStart, yStart, x, y);
                                        allValidMoves.Add(mov);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!piece.isWhite)
                        {
                            //Bottom left
                            int x = xStart - 1;
                            int y = yStart - 1;                       
                            if (xStart >= 1 && yStart >= 1)
                            {                                
                                if (piece.ValidMove(board, xStart, yStart, x, y))
                                {
                                    Move mov = new Move(xStart, yStart, x, y);
                                    allValidMoves.Add(mov);
                                }
                            }

                            //Bottom right
                            if (xStart <= 6 && yStart >= 1)
                            {
                                x = xStart + 1;
                                y = yStart - 1;
                                if (piece.ValidMove(board, xStart, yStart, x, y))
                                {
                                    Move mov = new Move(xStart, yStart, x, y);
                                    allValidMoves.Add(mov);
                                }
                            }

                            //If king
                            if (piece.isKing)
                            {
                                //Top left                            
                                if (xStart >= 1 && yStart <= 6)
                                {
                                    x = xStart - 1;
                                    y = yStart + 1;

                                    if (piece.ValidMove(board, xStart, yStart, x, y))
                                    {
                                        Move mov = new Move(xStart, yStart, x, y);
                                        allValidMoves.Add(mov);
                                    }
                                }

                                //Top right                                                       
                                if (xStart <= 6 && yStart <= 6)
                                {
                                    x = xStart + 1;
                                    y = yStart + 1;
                                    if (piece.ValidMove(board, xStart, yStart, x, y))
                                    {
                                        Move mov = new Move(xStart, yStart, x, y);
                                        allValidMoves.Add(mov);
                                    }
                                }
                            }
                        }
                    }
                }
                
            }

        return allValidMoves;
    }

    private void MakeMove (Piece[,] board, Move movimiento)
    {
        int xStart = movimiento.inicio.x;
        int yStart = movimiento.inicio.y;
        int x = movimiento.destino.x;
        int y = movimiento.destino.y;

        Piece piece = board[xStart, yStart];

        // Si se mata alguna eliminarla
        if (Mathf.Abs(xStart - x) == 2)
        {
            Piece p = board[(xStart + x) / 2, (yStart + y) / 2];
            if (p != null)
            {
                board[(xStart + x) / 2, (yStart + y) / 2] = null;
            }
        }

        // Actualizar el tablero
        board[x, y] = piece;
        board[xStart, yStart] = null;         
    }

    private double Minimax (Piece[,] board, int depth, bool whiteSide, bool maximizingPlayer, double alpha, double beta)
    {
        double initial = 0;
        Piece[,] tempBoard;

        if (depth == 0)
        {
            return GetHeuristic(board, whiteSide);
        }

        // Volver a calcular los movimientos posibles
        List<Move> possibleMoves = new List<Move>();
        List<Vector2> forcedPieces = new List<Vector2>();

        // Mirar si es obligatorio matar con alguna pieza
        forcedPieces = FindForcedPieces(board, whiteSide);

        // Si hay alguna incluir los movimientos posibles de ellas
        if (forcedPieces.Count > 0)
        {
            possibleMoves = GetForcedMoves(board, forcedPieces);
        }
        // Si no incluir todos los movimientos posibles
        else if (forcedPieces.Count == 0)
        {
            possibleMoves = GetAllValidMoves(board, whiteSide);
        }
        
        // Si el jugador busca MAX
        if (maximizingPlayer)
        {
            initial = double.NegativeInfinity;
            for (int i = 0; i < possibleMoves.Count; i++)
            {
                tempBoard = (Piece[,])board.Clone();
                MakeMove(tempBoard, possibleMoves[i]);

                double result = Minimax(tempBoard, depth - 1, !whiteSide, !maximizingPlayer, alpha, beta);

                if (result > initial)
                    initial = result;
                if (initial > alpha)
                    alpha = initial;             

                if (alpha >= beta)
                    break;
            }
        }
        // Si busca MIN
        else
        {
            initial = double.PositiveInfinity;
            for (int i = 0; i < possibleMoves.Count; i++)
            {
                tempBoard = (Piece[,])board.Clone();
                MakeMove(tempBoard, possibleMoves[i]);

                double result = Minimax(tempBoard, depth - 1, !whiteSide, !maximizingPlayer, alpha, beta);

                if (result < initial)
                    initial = result;
                if (initial < alpha)
                    alpha = initial;

                if (alpha >= beta)
                    break;
            }
        }

        return initial;
    }

    private double GetHeuristic (Piece[,] board, bool whiteSide)
    {
        double kingWeight = 1.5;
        double possibleKingWeight = 1.2;
        double result = 0;
        int numWhiteNormalPieces = 0;
        int numBlackNormalPieces = 0;
        int numBlackKingPieces = 0;
        int numWhiteKingPieces = 0;
        int possibleWhiteKing = 0;
        int possibleBlackKing = 0;

        // Contar el numero de piezas de cada tipo
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                Piece p = board[i, j];                
                if (p != null)
                {
                    if (p.isWhite)
                    {
                        if ((p.ValidMove(board, i, j, i + 1, 7)) || (p.ValidMove(board, i, j, i - 1, 7)) || (p.ValidMove(board, i, j, i + 2, 7)) || (p.ValidMove(board, i, j, i - 2, 7)))
                            possibleWhiteKing++;
                        if (p.isKing || j == 7)
                            numWhiteKingPieces++;
                        else
                            numWhiteNormalPieces++;
                    }
                    else
                    {
                        if ((p.ValidMove(board, i, j, i + 1, 0)) || (p.ValidMove(board, i, j, i - 1, 0)) || (p.ValidMove(board, i, j, i + 2, 0)) || (p.ValidMove(board, i, j, i - 2, 0)))
                            possibleBlackKing++;
                        if (p.isKing || j == 0)
                            numBlackKingPieces++;
                        else
                            numBlackNormalPieces++;
                    }         
                }
            }

        // Calcular el resultado
        if (whiteSide)
        {
            result = (numWhiteKingPieces * kingWeight + possibleWhiteKing * possibleKingWeight + numWhiteNormalPieces) 
                - (numBlackKingPieces * kingWeight + possibleBlackKing * possibleKingWeight + numBlackNormalPieces);
        }
        else
        {
            result = (numBlackKingPieces * kingWeight + possibleBlackKing * possibleKingWeight + numBlackNormalPieces)
                - (numWhiteKingPieces * kingWeight + possibleWhiteKing * possibleKingWeight + numWhiteNormalPieces);
        }

        return result;
    }
}