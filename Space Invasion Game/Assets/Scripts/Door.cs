using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private bool isLocked, isBroken, isOpen;
    [SerializeField] private GameObject lockedIndicator;
    [SerializeField] private GameObject obsticleCollider;

    List<uint> netIDList = new List<uint>();

    // Components
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (lockedIndicator.activeInHierarchy)
            lockedIndicator.SetActive(false);

        if (isLocked)
        {
            obsticleCollider.SetActive(true);
            animator.SetBool("isLocked", true);
            animator.Play("Locked");
            lockedIndicator.SetActive(true);
        }
        else if (isBroken) // added else if as to not cause a bug if isLocked and isBroken are true
        {
            obsticleCollider.SetActive(true);
            animator.SetBool("isBroken", true);
            animator.Play("Broken");
        }
        else
        {
            Unlock();
        }
    }

    public void Unlock()
    {
        animator.SetBool("isLocked", false);
        lockedIndicator.SetActive(false);
        obsticleCollider.SetActive(false);
    }

    public void Fix()
    {
        animator.SetBool("isBroken", false);
        obsticleCollider.SetActive(false);
    }

    public void Open()
    {
        if (!isLocked && !isBroken)
        {
            if (!isOpen)
            {
                animator.SetTrigger("OpenTrigger");
                isOpen = true;
            }
        }
    }

    public void Close()
    {
        if (!isLocked && !isBroken)
        {
            if (isOpen)
            {
                animator.SetTrigger("CloseTrigger");
                isOpen = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D otherCollision)
    {
        if(otherCollision.TryGetComponent<NetworkIdentity>(out NetworkIdentity identity))
        {
            if (!netIDList.Contains(identity.netId))
            {
                if (!isOpen)
                {
                    Open();
                }

                netIDList.Add(identity.netId);
            }
        }

        /*if (otherCollision.CompareTag("Interactive"))
        {
            if (!isOpen)
            {
                Open();
            }

            unitsInCollider++;
        }*/
    }

    private void OnTriggerExit2D(Collider2D otherCollision)
    {
        if (otherCollision.TryGetComponent<NetworkIdentity>(out NetworkIdentity identity))
        {
            if (netIDList.Contains(identity.netId))
            {
                netIDList.Remove(identity.netId);
            }

            if (netIDList.Count <= 0)
                Close();
        }

        /*if (otherCollision.CompareTag("Interactive"))
        {
            unitsInCollider--;

            if (unitsInCollider <= 0)
            {
                Close();
                unitsInCollider = 0;
            }
        }*/
    }
}
