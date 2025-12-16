using System.Collections.Generic;
using UnityEngine;

public class BoardSquareMap : MonoBehaviour
{
    [SerializeField] private BoardSquare[] squares;
    private Dictionary<(int,int), BoardSquare> _lookup;

    void Awake()
    {
        _lookup = new();
        foreach (var sq in squares)
        {
            if (sq == null) continue;
            _lookup[(sq.file,sq.rank)] = sq; 
        }
    }

    public BoardSquare GetSquare(int file, int rank)
    {
        _lookup.TryGetValue((file, rank), out var sq);
        return sq;
    }
}
