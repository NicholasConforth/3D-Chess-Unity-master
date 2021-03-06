using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; set; }
    private bool[,] allowedMoves { get; set; }

    public GameObject canvas;

    private const float TILE_SIZE = 1.0f;
    private const float TILE_OFFSET = 0.5f;

    private int selectionX = -1;
    private int selectionY = -1;

    public List<GameObject> chessmanPrefabs;
    private List<GameObject> activeChessman;

    private Quaternion whiteOrientation = Quaternion.Euler(0, 270, 0);
    private Quaternion blackOrientation = Quaternion.Euler(0, 90, 0);

    public Chessman[,] Chessmans { get; set; }
    private Chessman selectedChessman;

    public bool isWhiteTurn = true;

    private Material previousMat;
    public Material selectedMat;

    public int[] EnPassantMove { set; get; }

    public List<int> listPlacer;
    int max;
    int rook1;
    int rook2;
    // Use this for initialization
    bool is960 = false;
    bool isChess = false;
    bool kingSpawned = false;
    bool bishopsOnDiffSpace = false;
    bool rooksAreInPlace = false;

    void Start()
    {
        Instance = this;
        //SpawnAllChessmans();
        //SpawnAllRandomChessmans();
        EnPassantMove = new int[2] { -1, -1 };
    }

    public void testChess()
    {
        if (isChess)
        {
            Debug.Log("Chess loaded");
        }
        if (!isChess)
        {
            Debug.Log("Chess not loaded");
        }
    }

    public void onClick960()
    {
        if(is960)
        {
            Debug.Log("960 succesfully loaded");

        }else if(!is960)
        {
            Debug.Log("960 is not loaded");
        }
    }

    public void onClickKingSpawned()
    {
        if(kingSpawned)
        {
            Debug.Log("King spawned succesfully");
        }
        if(!kingSpawned)
        {
            Debug.Log("the rooks somehow spawned next to each other again huh?");
        }
    }

    public void onClickBishops()
    {
        if(bishopsOnDiffSpace)
        {
            Debug.Log("Bishops succefully loaded");
        }
        if(!bishopsOnDiffSpace)
        {
            Debug.Log("Bishops failed to load on a different space which means the second one does not exist.");
        }
    }

    public void onClickRooks()
    {
        if(rooksAreInPlace)
        {
            Debug.Log("Rooks either loaded succefully or the program is lying to me.");
        }
        if(!rooksAreInPlace)
        {
            Debug.Log("Rooks did not spawn.");
        }
    }

    public void Chess()
    {
        SpawnAllChessmans();
        canvas.SetActive(false);
    }
    public void onClickChess960()
    {
        SpawnAllRandomChessmans();
        canvas.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSelection();

        if (Input.GetMouseButtonDown(0))
        {
            if (selectionX >= 0 && selectionY >= 0)
            {
                if (selectedChessman == null)
                {
                    // Select the chessman
                    SelectChessman(selectionX, selectionY);
                }
                else
                {
                    // Move the chessman
                    MoveChessman(selectionX, selectionY);
                }
            }
        }

        if (Input.GetKey("escape"))
            Application.Quit();
    }

    private void SelectChessman(int x, int y)
    {
        if (Chessmans[x, y] == null) return;

        if (Chessmans[x, y].isWhite != isWhiteTurn) return;

        bool hasAtLeastOneMove = false;

        allowedMoves = Chessmans[x, y].PossibleMoves();
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (allowedMoves[i, j])
                {
                    hasAtLeastOneMove = true;
                    i = 8;
                    break;
                }
            }
        }

        if (!hasAtLeastOneMove)
            return;

        selectedChessman = Chessmans[x, y];
        previousMat = selectedChessman.GetComponent<MeshRenderer>().material;
        selectedMat.mainTexture = previousMat.mainTexture;
        selectedChessman.GetComponent<MeshRenderer>().material = selectedMat;

        BoardHighlights.Instance.HighLightAllowedMoves(allowedMoves);
    }

    private void MoveChessman(int x, int y)
    {
        if (allowedMoves[x, y])
        {
            Chessman c = Chessmans[x, y];

            if (c != null && c.isWhite != isWhiteTurn)
            {
                // Capture a piece

                if (c.GetType() == typeof(King))
                {
                    // End the game
                    EndGame();
                    return;
                }

                activeChessman.Remove(c.gameObject);
                Destroy(c.gameObject);
            }
            if (x == EnPassantMove[0] && y == EnPassantMove[1])
            {
                if (isWhiteTurn)
                    c = Chessmans[x, y - 1];
                else
                    c = Chessmans[x, y + 1];

                activeChessman.Remove(c.gameObject);
                Destroy(c.gameObject);
            }
            EnPassantMove[0] = -1;
            EnPassantMove[1] = -1;
            if (selectedChessman.GetType() == typeof(Pawn))
            {
                if(y == 7) // White Promotion
                {
                    activeChessman.Remove(selectedChessman.gameObject);
                    Destroy(selectedChessman.gameObject);
                    SpawnChessman(1, x, y, true);
                    selectedChessman = Chessmans[x, y];
                }
                else if (y == 0) // Black Promotion
                {
                    activeChessman.Remove(selectedChessman.gameObject);
                    Destroy(selectedChessman.gameObject);
                    SpawnChessman(7, x, y, false);
                    selectedChessman = Chessmans[x, y];
                }
                EnPassantMove[0] = x;
                if (selectedChessman.CurrentY == 1 && y == 3)
                    EnPassantMove[1] = y - 1;
                else if (selectedChessman.CurrentY == 6 && y == 4)
                    EnPassantMove[1] = y + 1;
            }

            Chessmans[selectedChessman.CurrentX, selectedChessman.CurrentY] = null;
            selectedChessman.transform.position = GetTileCenter(x, y);
            selectedChessman.SetPosition(x, y);
            Chessmans[x, y] = selectedChessman;
            isWhiteTurn = !isWhiteTurn;
        }

        selectedChessman.GetComponent<MeshRenderer>().material = previousMat;

        BoardHighlights.Instance.HideHighlights();
        selectedChessman = null;
    }

    private void UpdateSelection()
    {
        if (!Camera.main) return;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 50.0f, LayerMask.GetMask("ChessPlane")))
        {
            selectionX = (int)hit.point.x;
            selectionY = (int)hit.point.z;
        }
        else
        {
            selectionX = -1;
            selectionY = -1;
        }
    }

    private void SpawnChessman(int index, int x, int y, bool isWhite)
    {
        
        Vector3 position = GetTileCenter(x, y);
        GameObject go;

        if (isWhite)
        {
            go = Instantiate(chessmanPrefabs[index], position, whiteOrientation) as GameObject;
        }
        else
        {
            go = Instantiate(chessmanPrefabs[index], position, blackOrientation) as GameObject;
        }

        go.transform.SetParent(transform);
        Chessmans[x, y] = go.GetComponent<Chessman>();
        Chessmans[x, y].SetPosition(x, y);
        activeChessman.Add(go);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        Vector3 origin = Vector3.zero;
        origin.x += (TILE_SIZE * x) + TILE_OFFSET;
        origin.z += (TILE_SIZE * y) + TILE_OFFSET;

        return origin;
    }

    private void SpawnAllChessmans()
    {
        isChess = true;
        activeChessman = new List<GameObject>();
        Chessmans = new Chessman[8, 8];

        /////// White ///////

        // King
        SpawnChessman(0, 3, 0, true);

        // Queen
        SpawnChessman(1, 4, 0, true);

        // Rooks
        SpawnChessman(2, 0, 0, true);
        SpawnChessman(2, 7, 0, true);

        // Bishops
        SpawnChessman(3, 2, 0, true);
        SpawnChessman(3, 5, 0, true);

        // Knights
        SpawnChessman(4, 1, 0, true);
        SpawnChessman(4, 6, 0, true);

        // Pawns
        for (int i = 0; i < 8; i++)
        {
            SpawnChessman(5, i, 1, true);
        }


        /////// Black ///////

        // King
        SpawnChessman(6, 4, 7, false);

        // Queen
        SpawnChessman(7, 3, 7, false);

        // Rooks
        SpawnChessman(8, 0, 7, false);
        SpawnChessman(8, 7, 7, false);

        // Bishops
        SpawnChessman(9, 2, 7, false);
        SpawnChessman(9, 5, 7, false);

        // Knights
        SpawnChessman(10, 1, 7, false);
        SpawnChessman(10, 6, 7, false);

        // Pawns
        for (int i = 0; i < 8; i++)
        {
            SpawnChessman(11, i, 6, false);
        }
    }

    private void SpawnAllRandomChessmans()
    {
        Debug.Log("960 succesfully loaded");
        listPlacer = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
        max = listPlacer.Count;

        activeChessman = new List<GameObject>();
        Chessmans = new Chessman[8, 8];

        SpawnRooks();
        SpawnKings();
        SpawnBishops();
        SpawnQueens();
        SpawnKnights();
        Debug.Log("If the code get's here it is succsefully tried to randomize");

        // Pawns
        for (int i = 0; i < 8; i++)
        {
            SpawnChessman(5, i, 1, true);
        }
        // BlackPawns
        for (int i = 0; i < 8; i++)
        {
            SpawnChessman(11, i, 6, false);
        }
        is960 = true;
    }

    private void SpawnRooks()
    {
        var rand = new System.Random();
        int place = rand.Next(0, max);

        // Rooks
        SpawnChessman(2, listPlacer[place], 0, true);
        SpawnChessman(8, listPlacer[place], 7, false);
        rook1 = place;

        listPlacer.RemoveAt(place);
        max = listPlacer.Count;
        bool loop = true;
        do
        {
            int place2 = rand.Next(0, max);
            if (place2 != place + 1 && place2 != place - 1)
            {
                SpawnChessman(2, listPlacer[place2], 0, true);
                SpawnChessman(8, listPlacer[place2], 7, false);
                rooksAreInPlace = true;
                listPlacer.RemoveAt(place2);
                max = listPlacer.Count;
                rook2 = place2;
                loop = false;
            }
        } while (loop);
    }

    private void SpawnKings()
    {
        var rand = new System.Random();
        int place;
        if(rook1 > rook2)
        {
            place = rand.Next(rook2, rook1 - 1);
            if ((listPlacer[place] > rook1 && listPlacer[place] < rook2) || (listPlacer[place] < rook1 && listPlacer[place] > rook2))
            {
                SpawnChessman(0, listPlacer[place], 0, true);
                SpawnChessman(6, listPlacer[place], 7, false);
                kingSpawned = true;
                listPlacer.RemoveAt(place);
                max = listPlacer.Count;

            }
        }else if(rook1 < rook2)
            {
            place = rand.Next(rook1, rook2 - 1);
            if ((listPlacer[place] > rook1 && listPlacer[place] < rook2) || (listPlacer[place] < rook1 && listPlacer[place] > rook2))
            {
                SpawnChessman(0, listPlacer[place], 0, true);
                SpawnChessman(6, listPlacer[place], 7, false);
                kingSpawned = true;
                listPlacer.RemoveAt(place);
                max = listPlacer.Count;
            }
        }
    }

    private void SpawnBishops()
    {
        var rand = new System.Random();
        int place = rand.Next(0, max);
        SpawnChessman(3, listPlacer[place], 0, true);
        SpawnChessman(9, listPlacer[place], 7, false);
        listPlacer.RemoveAt(place);
        max = listPlacer.Count;
        bool loop = true;
        do
        {
            int place2 = rand.Next(0, max);
            if ((place % 2 == 0 && place2 % 2 == 1) || (place % 2 == 1 && place2 % 2 == 0))
            {
                SpawnChessman(3, listPlacer[place2], 0, true);
                SpawnChessman(9, listPlacer[place2], 7, false);
                bishopsOnDiffSpace = true;
                listPlacer.RemoveAt(place2);
                max = listPlacer.Count;
                loop = false;
            }
        } while (loop);
    }

    private void SpawnQueens()
    {
        var rand = new System.Random();
        int place = rand.Next(0, max);
        SpawnChessman(1, listPlacer[place], 0, true);
        SpawnChessman(7, listPlacer[place], 7, false);
        Debug.Log("queen is at is at: " + listPlacer[place]);
        listPlacer.RemoveAt(place);
        max = listPlacer.Count;

    }

    private void SpawnKnights()
    {
        var rand = new System.Random();
        int place = rand.Next(0, max);
        SpawnChessman(4, listPlacer[place], 0, true);
        SpawnChessman(10, listPlacer[place], 7, false);
        Debug.Log("knight one is at is at: " + listPlacer[place]);
        listPlacer.RemoveAt(place);
        max = listPlacer.Count;

        place = rand.Next(0, max);
        SpawnChessman(4, listPlacer[place], 0, true);
        SpawnChessman(10, listPlacer[place], 7, false);
        Debug.Log("knight two is at is at: " + listPlacer[place]);
    }

    private void EndGame()
    {
        if (isWhiteTurn)
            Debug.Log("White wins");
        else
            Debug.Log("Black wins");

        foreach (GameObject go in activeChessman)
        {
            Destroy(go);
        }

        is960 = false;
        isChess = false;
        kingSpawned = false;
        bishopsOnDiffSpace = false;
        rooksAreInPlace = false;
        isWhiteTurn = true;
        BoardHighlights.Instance.HideHighlights();
        canvas.SetActive(true);
        //SpawnAllChessmans();
        //SpawnAllRandomChessmans();
    }
}


