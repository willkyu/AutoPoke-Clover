using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "UI/Color Palette")]
public class ColorPalette : ScriptableObject
{
    public Color primaryColor = new Color32(197, 240, 23, 255);     // #C5F017
    public Color secondaryColor = new Color32(236, 234, 255, 255);  // #ECEAFF
    public Color backgroundColor = new Color32(250, 250, 251, 255); // #FAFAFB
    public Color textColor = new Color32(51, 51, 51, 255);          // #333333
    public Color accentPurple = new Color32(108, 95, 254, 255);     // #6C5FFE
    public Color successColor = new Color32(63, 201, 92, 255);      // #3FC95C
}
