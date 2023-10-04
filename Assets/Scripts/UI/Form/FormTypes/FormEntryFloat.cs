using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FormEntryFloat : FormEntry<float>
{
    public TMP_InputField Field;

    public new void Start() 
    {
        base.Start();
        Reset();
    }

    public void Reset()
    {
        recursionBuster = true;
        Field.text = CurrentValue.ToString();
        recursionBuster = false;
    }
    
    public void SetValue(string value)
    {
        if (float.TryParse(value, out float v)) SetValue(v);
    }
}
