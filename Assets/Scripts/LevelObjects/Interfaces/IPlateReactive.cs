using UnityEngine;

public interface IPlateReactive
{
    // PressurePlate basıldığında çağrılır
    void PlatePressed(PressurePlate plate);

    // PressurePlate boşaldığında çağrılır
    void PlateReleased(PressurePlate plate);
}
