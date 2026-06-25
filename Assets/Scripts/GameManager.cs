using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Rigidbody[] dominoes;

    bool started;

    public void StartChain()
    {
        started = true;

        foreach (Rigidbody rb in dominoes)
        {
            rb.isKinematic = false;
        }

        dominoes[0].AddForce(
            Vector3.right * 5f,
            ForceMode.Impulse);
        //dominoes[0].AddTorque(
        //Vector3.forward * -10f,
        //ForceMode.Impulse);
    }

    void Update()
    {
        if (!started)
            return;

        bool allFallen = true;

        foreach (Rigidbody rb in dominoes)
        {
            Domino domino = rb.GetComponent<Domino>();

            if (!domino.HasFallen)
            {
                allFallen = false;
                break;
            }
        }

        if (allFallen)
        {
            Debug.Log("LEVEL COMPLETE");
            started = false;
        }
    }
}