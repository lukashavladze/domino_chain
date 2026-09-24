using System.Collections.Generic;
using UnityEngine;

public class DominoBlockingCalculator : MonoBehaviour
{
    [Header("Blocking Detection")]

    [Tooltip("Extra space around the simulated fallen domino.")]
    [Range(0f, 0.1f)]
    public float padding = 0.01f;

    [Range(0.5f, 1.1f)]
    public float fallReachMultiplier = 0.90f;

    [Range(0.01f, 0.25f)]
    public float landingTolerance = 0.08f;

    [Range(0.1f, 1.0f)]
    public float widthMultiplier = 0.45f;

    [Header("Debug")]
    public bool logDependencies = false;




    public void CalculateBlocking(
        List<DominoLine> lines)
    {
        if (lines == null)
            return;

        // ==========================================
        // CLEAR OLD DEPENDENCIES
        // ==========================================

        foreach (DominoLine line in lines)
        {
            if (line == null)
                continue;

            line.ClearGeneratedBlocking();
            line.useGeneratedBlocking = true;
        }

        // ==========================================
        // CHECK EACH LINE
        // ==========================================

        foreach (DominoLine targetLine in lines)
        {
            if (targetLine == null)
                continue;

            foreach (DominoLine otherLine in lines)
            {
                if (otherLine == null ||
                    otherLine == targetLine)
                {
                    continue;
                }

                // IMPORTANT:
                //
                // We ONLY simulate targetLine's
                // last domino falling.
                //
                // otherLine stays standing.

                if (DoesLastDominoHitLine(
                    targetLine,
                    otherLine))
                {
                    targetLine.AddBlocker(
                        otherLine);

                    if (logDependencies)
                    {
                        Debug.Log(
                            targetLine.name +
                            " BLOCKED BY " +
                            otherLine.name);
                    }
                }
            }
        }
    }

    private bool BoxesOverlapXZ(
    Vector3 centerA,
    Vector3 halfA,
    Quaternion rotationA,
    Vector3 centerB,
    Vector3 halfB,
    Quaternion rotationB)
    {
        Vector3 aRight =
            rotationA * Vector3.right;

        Vector3 aForward =
            rotationA * Vector3.forward;

        Vector3 bRight =
            rotationB * Vector3.right;

        Vector3 bForward =
            rotationB * Vector3.forward;

        aRight.y = 0f;
        aForward.y = 0f;
        bRight.y = 0f;
        bForward.y = 0f;

        aRight.Normalize();
        aForward.Normalize();
        bRight.Normalize();
        bForward.Normalize();

        Vector3 difference =
            centerB - centerA;

        difference.y = 0f;

        if (IsSeparated(
            difference,
            aRight,
            aRight,
            aForward,
            halfA.x,
            halfA.z,
            bRight,
            bForward,
            halfB.x,
            halfB.z))
        {
            return false;
        }

        if (IsSeparated(
            difference,
            aForward,
            aRight,
            aForward,
            halfA.x,
            halfA.z,
            bRight,
            bForward,
            halfB.x,
            halfB.z))
        {
            return false;
        }

        if (IsSeparated(
            difference,
            bRight,
            aRight,
            aForward,
            halfA.x,
            halfA.z,
            bRight,
            bForward,
            halfB.x,
            halfB.z))
        {
            return false;
        }

        if (IsSeparated(
            difference,
            bForward,
            aRight,
            aForward,
            halfA.x,
            halfA.z,
            bRight,
            bForward,
            halfB.x,
            halfB.z))
        {
            return false;
        }

        return true;
    }

    private bool DoesLastDominoHitLine(
    DominoLine fallingLine,
    DominoLine standingLine)
    {
        if (fallingLine == null ||
            standingLine == null)
        {
            return false;
        }

        if (fallingLine.dominoes == null ||
            fallingLine.dominoes.Count < 2)
        {
            return false;
        }

        if (standingLine.dominoes == null ||
            standingLine.dominoes.Count == 0)
        {
            return false;
        }

        Domino last =
            fallingLine.dominoes[
                fallingLine.dominoes.Count - 1];

        Domino previous =
            fallingLine.dominoes[
                fallingLine.dominoes.Count - 2];

        if (last == null || previous == null)
            return false;

        BoxCollider lastBox =
            last.GetComponentInChildren<BoxCollider>();

        if (lastBox == null)
            return false;

        // ============================================
        // DIRECTION LAST DOMINO FALLS
        // ============================================

        Vector3 direction =
            last.transform.position -
            previous.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return false;

        direction.Normalize();

        // ============================================
        // REAL DOMINO HEIGHT
        // ============================================

        Vector3 scaledSize =
            Vector3.Scale(
                lastBox.size,
                lastBox.transform.lossyScale);

        float height =
            Mathf.Abs(scaledSize.y);

        // Don't use the complete theoretical height.
        //
        // We want the useful contact/landing region,
        // not a huge conservative blocking rectangle.
        float reach =
    height *
    fallReachMultiplier;

        // ============================================
        // LANDING POINT OF TOP OF FALLING DOMINO
        // ============================================

        Vector3 landingPoint =
            last.transform.position +
            direction * reach;

        landingPoint.y =
            last.transform.position.y;

        // Width of the domino across the line.
        float dominoWidth =
            Mathf.Max(
                Mathf.Abs(scaledSize.x),
                Mathf.Abs(scaledSize.z));

        float lateralTolerance =
    dominoWidth *
    widthMultiplier;


        // ============================================
        // CHECK OTHER LINE
        // ============================================

        foreach (Domino other in standingLine.dominoes)
        {
            if (other == null)
                continue;

            Vector3 toOther =
                other.transform.position -
                last.transform.position;

            toOther.y = 0f;

            // Distance forward from our last domino.
            float forwardDistance =
                Vector3.Dot(
                    toOther,
                    direction);

            // Ignore anything behind our last domino.
            if (forwardDistance <= 0f)
                continue;

            // Perpendicular distance from fall line.
            Vector3 projectedPoint =
                last.transform.position +
                direction * forwardDistance;

            Vector3 lateral =
                other.transform.position -
                projectedPoint;

            lateral.y = 0f;

            float lateralDistance =
                lateral.magnitude;

            // ========================================
            // OTHER DOMINO MUST BE NEAR LANDING POINT
            // ========================================

            bool nearLandingDistance =
                Mathf.Abs(
                    forwardDistance - reach)
                <= landingTolerance;

            bool insideWidth =
                lateralDistance
                <= lateralTolerance;

            if (nearLandingDistance &&
                insideWidth)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 GetWorldHalfSize(
    BoxCollider box)
    {
        Vector3 size =
            Vector3.Scale(
                box.size,
                box.transform.lossyScale);

        return new Vector3(
            Mathf.Abs(size.x) * 0.5f,
            Mathf.Abs(size.y) * 0.5f,
            Mathf.Abs(size.z) * 0.5f);
    }

    private bool FallenBoxIntersectsStandingBox(
        Vector3 fallenCenter,
        Vector3 fallenHalfExtents,
        Quaternion fallenRotation,
        BoxCollider standingBox)
    {
        // ==========================================
        // GET STANDING COLLIDER WORLD DATA
        // ==========================================

        Transform t =
            standingBox.transform;

        Vector3 standingCenter =
            t.TransformPoint(
                standingBox.center);

        Vector3 standingSize =
            Vector3.Scale(
                standingBox.size,
                t.lossyScale);

        Vector3 standingHalfExtents =
            new Vector3(
                Mathf.Abs(standingSize.x) * 0.5f,
                Mathf.Abs(standingSize.y) * 0.5f,
                Mathf.Abs(standingSize.z) * 0.5f);

        Quaternion standingRotation =
            t.rotation;

        // ==========================================
        // TEST USING SAT IN XZ PLANE
        // ==========================================

        return OrientedRectanglesOverlapXZ(
            fallenCenter,
            fallenHalfExtents,
            fallenRotation,
            standingCenter,
            standingHalfExtents,
            standingRotation);
    }

    private bool OrientedRectanglesOverlapXZ(
        Vector3 centerA,
        Vector3 halfA,
        Quaternion rotationA,
        Vector3 centerB,
        Vector3 halfB,
        Quaternion rotationB)
    {
        Vector3 aRight =
            rotationA * Vector3.right;

        Vector3 aForward =
            rotationA * Vector3.forward;

        Vector3 bRight =
            rotationB * Vector3.right;

        Vector3 bForward =
            rotationB * Vector3.forward;

        aRight.y = 0f;
        aForward.y = 0f;
        bRight.y = 0f;
        bForward.y = 0f;

        aRight.Normalize();
        aForward.Normalize();
        bRight.Normalize();
        bForward.Normalize();

        Vector3 difference =
            centerB - centerA;

        difference.y = 0f;

        // Separating Axis Theorem.
        // If separated on ANY axis,
        // rectangles don't overlap.

        if (IsSeparated(
            difference,
            aRight,
            aRight,
            aForward,
            halfA.x,
            halfA.z,
            bRight,
            bForward,
            halfB.x,
            halfB.z))
        {
            return false;
        }

        if (IsSeparated(
            difference,
            aForward,
            aRight,
            aForward,
            halfA.x,
            halfA.z,
            bRight,
            bForward,
            halfB.x,
            halfB.z))
        {
            return false;
        }

        if (IsSeparated(
            difference,
            bRight,
            aRight,
            aForward,
            halfA.x,
            halfA.z,
            bRight,
            bForward,
            halfB.x,
            halfB.z))
        {
            return false;
        }

        if (IsSeparated(
            difference,
            bForward,
            aRight,
            aForward,
            halfA.x,
            halfA.z,
            bRight,
            bForward,
            halfB.x,
            halfB.z))
        {
            return false;
        }

        // No separating axis found.
        return true;
    }

    private bool IsSeparated(
        Vector3 centerDifference,
        Vector3 axis,
        Vector3 aRight,
        Vector3 aForward,
        float aHalfRight,
        float aHalfForward,
        Vector3 bRight,
        Vector3 bForward,
        float bHalfRight,
        float bHalfForward)
    {
        float centerDistance =
            Mathf.Abs(
                Vector3.Dot(
                    centerDifference,
                    axis));

        float radiusA =
            aHalfRight *
            Mathf.Abs(
                Vector3.Dot(
                    aRight,
                    axis))
            +
            aHalfForward *
            Mathf.Abs(
                Vector3.Dot(
                    aForward,
                    axis));

        float radiusB =
            bHalfRight *
            Mathf.Abs(
                Vector3.Dot(
                    bRight,
                    axis))
            +
            bHalfForward *
            Mathf.Abs(
                Vector3.Dot(
                    bForward,
                    axis));

        return centerDistance >
               radiusA + radiusB;
    }
}