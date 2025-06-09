using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    private Animator animator;
    private PlayerBase playerBase;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerBase = GetComponent<PlayerBase>();
    }
}
