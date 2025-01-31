﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class Mirror : MonoBehaviour
{
    private Vector3 hitPos;
    private Vector3 normalVector;
    private Vector3 newestVector;
    public float power;
    private Renderer rend;
    private bool isTriggered = false;
    private float setDelayTrigger = 0f;
    public float minTimeDelay;
    
    public void Start()
    {
        rend = GetComponent<Renderer>();
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Hitbox2D")
        {
            return;
        }
        if (isTriggered == false)
        {
            isTriggered = true;
            hitPos = other.ClosestPointOnBounds(transform.position);
            normalVector = hitPos - transform.position;
            normalVector.y = 0f;
            newestVector = Vector3.Normalize(Vector3.Reflect(other.GetComponent<Rigidbody>().velocity, normalVector));
            if (newestVector == null)
            {
                return;
            }

            newestVector.y = 0f;
            ///todo network this movement
            other.transform.GetComponentInParent<Rigidbody>().AddForce(newestVector * power, ForceMode.Impulse);
            rend.material.SetColor("_BaseColor", Random.ColorHSV());
        }
    }

    public void FixedUpdate()
    {
        if (isTriggered)
        {
            setDelayTrigger = setDelayTrigger + Time.deltaTime;
            if (setDelayTrigger > minTimeDelay)
            {
                isTriggered = false;
                setDelayTrigger = 0f;
            }
            else
            {
                return;
            }
        }
    }
}