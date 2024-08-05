using System.Collections;
using System.Collections.Generic;

namespace SimplePlayer.Entities;

public class SoundBoard
{
    public string Name { get; set; }
    public int Columns { get; set; }
    public int Rows { get; set; }
    public List<Sound> Sounds { get; set; }
}