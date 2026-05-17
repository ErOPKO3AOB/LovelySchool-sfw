using UnityEngine;

public class Chain : MonoBehaviour
{
    [SerializeField] private Transform[] chainParts;
    [SerializeField] private float speed = 5f;

    private Vector3[] positions;
    private Quaternion[] rotations;

    private int offset;

    private void Start()
    {
        positions = new Vector3[chainParts.Length];
        rotations = new Quaternion[chainParts.Length];

        for (int i = 0; i < chainParts.Length; i++)
        {
            positions[i] = chainParts[i].localPosition;
            rotations[i] = chainParts[i].localRotation;
        }
    }

    private void Update()
    {
        bool completed = true;

        for (int i = 0; i < chainParts.Length; i++)
        {
            int target = (i + offset + 1) % chainParts.Length;

            chainParts[i].localPosition = Vector3.MoveTowards(
                chainParts[i].localPosition,
                positions[target],
                speed * Time.deltaTime
            );

            chainParts[i].localRotation = Quaternion.RotateTowards(
                chainParts[i].localRotation,
                rotations[target],
                speed * 200f * Time.deltaTime
            );

            if (Vector3.Distance(
                    chainParts[i].localPosition,
                    positions[target]) > 0.001f)
            {
                completed = false;
            }
        }

        // когда ВСЕ дошли — двигаем цикл
        if (completed)
        {
            offset++;

            if (offset >= chainParts.Length)
                offset = 0;
        }
    }
}