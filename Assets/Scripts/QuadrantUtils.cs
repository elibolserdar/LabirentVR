using UnityEngine;

public enum MazeQuadrant
{
    NE,
    NW,
    SW,
    SE
}

public enum CardinalPoint
{
    N,
    S,
    E,
    W
}

public static class QuadrantUtils
{
    public static MazeQuadrant GetQuadrant(
        Vector3 position,
        Vector3 poolCenter)
    {
        Vector3 localPosition = position - poolCenter;

        if (localPosition.x >= 0f)
            return localPosition.z >= 0f
                ? MazeQuadrant.NE
                : MazeQuadrant.SE;

        return localPosition.z >= 0f
            ? MazeQuadrant.NW
            : MazeQuadrant.SW;
    }

    public static Vector3 GetQuadrantDirection(
        MazeQuadrant quadrant)
    {
        return quadrant switch
        {
            MazeQuadrant.NE => new Vector3(1f, 0f, 1f).normalized,
            MazeQuadrant.NW => new Vector3(-1f, 0f, 1f).normalized,
            MazeQuadrant.SW => new Vector3(-1f, 0f, -1f).normalized,
            MazeQuadrant.SE => new Vector3(1f, 0f, -1f).normalized,
            _ => Vector3.forward
        };
    }

    public static Vector3 GetCardinalStartPosition(
        CardinalPoint cardinalPoint,
        Vector3 poolCenter,
        float poolRadius,
        float edgeInset = 0.5f)
    {
        float distanceFromCenter =
            Mathf.Max(0f, poolRadius - edgeInset);

        Vector3 direction = cardinalPoint switch
        {
            CardinalPoint.N => Vector3.forward,
            CardinalPoint.S => Vector3.back,
            CardinalPoint.E => Vector3.right,
            CardinalPoint.W => Vector3.left,
            _ => Vector3.forward
        };

        return poolCenter + direction * distanceFromCenter;
    }

    public static Quaternion GetRotationFacingCenter(
        Vector3 startPosition,
        Vector3 poolCenter)
    {
        Vector3 direction = poolCenter - startPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return Quaternion.identity;

        return Quaternion.LookRotation(
            direction.normalized,
            Vector3.up);
    }

    public static float CalculateHeadingError(
        Vector3 startPosition,
        Vector3 currentPosition,
        Vector3 platformPosition)
    {
        Vector3 idealDirection =
            platformPosition - startPosition;

        Vector3 actualDirection =
            currentPosition - startPosition;

        idealDirection.y = 0f;
        actualDirection.y = 0f;

        if (idealDirection.sqrMagnitude < 0.0001f ||
            actualDirection.sqrMagnitude < 0.0001f)
        {
            return 0f;
        }

        return Vector3.Angle(
            idealDirection,
            actualDirection);
    }

    public static Vector3 GetProbeStartPosition(
        Vector3 platformPosition,
        Vector3 poolCenter,
        float poolRadius,
        float edgeInset = 0.5f)
    {
        Vector3 platformDirection =
            platformPosition - poolCenter;

        platformDirection.y = 0f;

        if (platformDirection.sqrMagnitude < 0.0001f)
            return poolCenter;

        Vector3 oppositeDirection =
            -platformDirection.normalized;

        float distanceFromCenter =
            Mathf.Max(0f, poolRadius - edgeInset);

        return poolCenter +
               oppositeDirection * distanceFromCenter;
    }
}