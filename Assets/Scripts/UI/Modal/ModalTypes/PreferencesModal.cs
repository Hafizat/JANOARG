using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreferencesModal : Modal
{
    public static PreferencesModal main;

    public RectTransform FormHolder;
    public Button[] TabButtons;

    public bool IsDirty = false;

    public void Awake()
    {
        if (main) Close();
        else main = this;
    }

    public void OnDestroy()
    {
        if (IsDirty) Chartmaker.main.StartSavePrefsRoutine();
    }

    public new void Start()
    {
        base.Start();
        SetTab(0);
    }

    public void SetTab(int tab)
    {
        for (int a = 0; a < TabButtons.Length; a++) 
        {
            TabButtons[a].interactable = tab != a;
        }

        ClearForm();

        if (tab == 0)
        {
            var prefs = Chartmaker.main.Preferences;
            var storage = Chartmaker.main.PreferencesStorage;
            SpawnForm<FormEntryHeader>("Auto-Save");
            SpawnForm<FormEntryBool, bool>("Save on Play", () => prefs.SaveOnPlay, x => {
                storage.Set("AS:SaveOnPlay", prefs.SaveOnPlay = x); IsDirty = true;
            });
            SpawnForm<FormEntryBool, bool>("Save on Quit", () => prefs.SaveOnQuit, x => {
                storage.Set("AS:SaveOnQuit", prefs.SaveOnQuit = x); IsDirty = true;
            });
        }
        else if (tab == 1)
        {
            SpawnForm<FormEntryLabel>("Note: These are not editable for now, consider this as a reference sheet.");
            var categories = KeyboardHandler.main.Keybindings.MakeCategoryGroups();
            foreach (var cat in categories)
            {
                SpawnForm<FormEntryHeader>(cat.Key);
                foreach (var entry in cat.Value)
                {
                    var field = SpawnForm<FormEntryString, string>(entry.Value.Name, () => entry.Value.Keybind.ToString(), x => {});
                    field.Field.interactable = false;
                }
            }
        }
    }
    
    public void ClearForm()
    {
        foreach (RectTransform rt in FormHolder)
        {
            Destroy(rt.gameObject);
        }
    }

    T SpawnForm<T>(string title = "") where T : FormEntry
        => Formmaker.main.Spawn<T>(FormHolder, title);

    T SpawnForm<T, U>(string title, Func<U> get, Action<U> set) where T : FormEntry<U>
        => Formmaker.main.Spawn<T, U>(FormHolder, title, get, set);
}
