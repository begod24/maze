---
name: add-ui-screen
description: Add a UI Toolkit screen or HUD element to No Way Out — UXML/USS in Assets/_Project/UI, View (MonoBehaviour) + Presenter (plain C#), prefab with UIDocument, IScreenService/UIScreensConfig, EN+RU strings in the Localization tables, USS transitions for animation, and the HUD truth rule. The first screen also builds the UI foundation (section 0).
---

# Adding a UI screen (UI Toolkit)

Read `Docs/TechnicalOverview.md` §15–16 and `Docs/GDD.md` §11. For detailed UXML/USS syntax, the `unity:ui-uitk` skill can help, but **this project's structure wins** when they disagree.

## 0. Foundation [exists since the main menu]

All of this is built. Reuse it and only extend it (new tokens in `Variables.uss`, shared classes in `Common.uss`). Fonts: Oswald (`.text--title`) and IBM Plex Mono (`.text`, buttons), both with dynamic SDF font assets in `UI/Fonts/`. For reference, the foundation is:
- `Assets/_Project/UI/Panel/GamePanelSettings.asset`: PanelSettings, Scale With Screen Size, 1920×1080, match 0.5, a theme `GameTheme.tss` (default runtime theme + `Styles/Common.uss`).
- `Assets/_Project/UI/Styles/Variables.uss`: `:root` tokens for colors (`--color-bg`, `--color-text`, `--color-accent`, `--color-danger`), font sizes, spacing, and durations (`--duration-fast`, `--duration-normal`).
- `Assets/_Project/UI/Styles/Common.uss` with `.screen` (full-size, `transition-property: opacity, translate; transition-duration: var(--duration-normal)`), `.screen--hidden` (`opacity: 0; translate: 0 16px; display` handled by code after the transition), and buttons and labels.
- A font with **Cyrillic** in `UI/Fonts/`, set in the theme.
- `NoWayOut.UI/Common/`:
  - `IScreenService` (public): `Show<TView>()`, `Hide<TView>()`, `IsVisible<TView>()`,
  - `ScreenService` (internal): instantiates view prefabs through `IObjectResolver.Instantiate`, caches them per scope, toggles `.screen--hidden`,
  - `UIScreensConfig` (public ScriptableObject): the list of view prefabs.
- `UIInstaller` registers `IScreenService` (Scoped) and receives `UIScreensConfig` through its constructor. The scope that calls it holds the config, and a `MainMenuLifetimeScope` gets created in `MainMenu.unity` if the menu needs UI.
- If clicks don't arrive, add an `EventSystem` with `InputSystemUIInputModule` to the scene, because the project uses the Input System only.

## 1. Files for a screen `<Screen>`

| File | Where |
|---|---|
| `<Screen>.uxml`, `<Screen>.uss` | `Assets/_Project/UI/Screens/<Screen>/` |
| `<Screen>View.cs` (`public sealed : MonoBehaviour`) | `Runtime/UI/Screens/<Screen>/` (HUD: `Runtime/UI/HUD/`) |
| `<Screen>Presenter.cs` (`internal sealed`) | same folder |
| `<Screen>.prefab` (`UIDocument` + View) | `Assets/_Project/Prefabs/UI/` |

- **UXML:** a root `VisualElement` with class `screen`. Element `name`s are kebab-case (`play-button`). No inline styles. The screen's USS imports only variables and uses `var(--...)`.
- **View:**
  - queries elements in `OnEnable` (`root.Q<Button>("play-button")`),
  - registers callbacks and unregisters them in `OnDisable`,
  - raises C# events (`public event Action PlayClicked;`),
  - exposes `Set...` methods.
  
  It has no services and no logic. It gets the Presenter through `[Inject] private void Construct(<Screen>Presenter presenter)` and calls `presenter.Attach(this)` / `presenter.Detach()` in `OnEnable`/`OnDisable`.
- **Presenter:** constructor-injected services and the event bus. It subscribes to View events and service/bus events, and pushes data into the View. Register it (Scoped) in the installer of the scene that uses it: `MainMenuInstaller` for menu screens, `UIInstaller` for screens every scene needs.
- After an `await`, a presenter checks that its view is still attached (`view != null`): the scene may have unloaded meanwhile.
- **Prefab:** a `UIDocument` with `GamePanelSettings`, the UXML as source, and `sortingOrder` (HUD 0, menus 10, popups 20). Add the prefab to `UIScreensConfig`.

## 2. Text: EN + RU, always

- Keys go in the `UI` table, snake_case, `<screen>.<element>`: `main_menu.play`, `lobby.join_code`. Entity and item names go in `Content`.
- Bind in the View: `playButton.SetBinding("text", new LocalizedString("UI", "main_menu.play"));`
- Text with values uses a smart string entry (`{code}`) plus a `LocalizedString` with local variables, updated by the View's `Set...` method.
- Add entries in **both** locales through `Unity_RunCommand` (never edit table YAML):
  ```csharp
  var collection = global::UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollection("UI");
  var en = (global::UnityEngine.Localization.Tables.StringTable)collection.GetTable("en");
  var ru = (global::UnityEngine.Localization.Tables.StringTable)collection.GetTable("ru");
  en.AddEntry("main_menu.play", "Play");
  ru.AddEntry("main_menu.play", "Играть");
  EditorUtility.SetDirty(en);
  EditorUtility.SetDirty(ru);
  EditorUtility.SetDirty(en.SharedData);
  AssetDatabase.SaveAssets();
  ```
- If you aren't sure of the Russian or English wording, ask the user and offer 2–3 variants.

## 3. Showing it

- `IScreenService` is Scoped per scene scope, so **the scene's own presenters** (or that scene's state machine) call `screenService.Show<<Screen>View>()` / `Hide`. Root App states can't resolve it. Example: `MainMenuPresenter` shows `JoinCodeView`, `ComingSoonView` and `LobbyView`.
- A UI that lives in 3D (like the menu TVs) puts transparent UI Toolkit buttons over the world objects (project their bounds each frame), so input, hover and focus stay plain UI Toolkit events. See `MainMenuView`.
- A button that should change the flow calls a **service** from the Presenter (e.g. `ISessionService.HostAsync`) or publishes an intent event. It never calls App.

## 4. Animation

- Show, hide, hover and press go in USS transitions using duration variables.
- Anything USS can't do (sequences, number counters) uses PrimeTween (`Tween.Custom`), stored in a field and stopped in `OnDisable`.

## 5. HUD truth rule (GDD §11)

No element may reveal what illusions hide: no distance to teammates, no true "who is speaking" indicator, no nameplates. If a HUD element shows information about other players, ask the user how illusions should fake it **before** building it.

## 6. Verify and report

- Run `unity-compile-check`.
- Preview without Play Mode by opening the UXML in UI Builder (tell the user), or by rendering the prefab in a preview scene.
- In the report, list the UXML/USS, View/Presenter, prefab, `UIScreensConfig` and the localization tables (`UI_en`, `UI_ru`, `UI Shared Data`).
