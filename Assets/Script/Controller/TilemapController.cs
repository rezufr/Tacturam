using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilemapController : MonoBehaviour
{
    public Tile[] Tile;

    void Start()
    {
        
    }

    public void CalculateTilemapMove()
    {
       for (int i = 0; i < Tile.Length; i++)
        {
            if (Tile[i].isActive == true)
            {
                
            }
        } 
    }

    public void CheckTileType()
    {
        
    }

    public void BlockTile(Tile tile)
    {
        
    }

    public void TelegraphTile(Tile tile)
    {
        
    }

    public void MoveTile(Tile tile)
    {
        
    }
}
