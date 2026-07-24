using System.Collections.Generic;
using UnityEngine;

public class DominoLine : MonoBehaviour
{
    public Domino firstDomino;

    public List<Domino> dominoes = new();

    public void AutoConnect()
    {
        dominoes.Clear();

        foreach (Transform child in transform)
        {
            Domino domino = child.GetComponent<Domino>();

            if (domino != null)
            {
                dominoes.Add(domino);
            }
        }

        if (dominoes.Count == 0)
        {
            firstDomino = null;
            return;
        }

        firstDomino = dominoes[0];

        for (int i = 0; i < dominoes.Count; i++)
        {
            Domino domino = dominoes[i];

            domino.canStartChain = (i == 0);

            if (i < dominoes.Count - 1)
                domino.nextDomino = dominoes[i + 1];
            else
                domino.nextDomino = null;
        }

        Debug.Log("Auto Connected " + dominoes.Count + " dominoes.");
    }

    public void StartLine()
    {
        if (firstDomino == null)
            return;

        firstDomino.Fall(transform.right);
    }

    public void ResetLine()
    {
        foreach (Domino domino in dominoes)
        {
            domino.ResetDomino();
        }
    }
}