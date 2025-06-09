using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHurt : MonoBehaviour
{
    private SpriteRenderer sp;

    private void Awake()
    {
        sp = GetComponent<SpriteRenderer>();
    }

    public void Hurt()
    {

        StartCoroutine(Red());
    }

    IEnumerator Red()
    {
        int count = 3;
        while (count > 0)
        {
            count--;
            sp.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            sp.color = Color.white;
            yield return new WaitForSeconds(0.2f);
        }
    }
}