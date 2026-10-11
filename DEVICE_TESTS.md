# Device acceptance tests
Record device model, OS, SDK/workload version, build command, date, actual result and screenshot filename for every scenario. These are expected outcomes, not observed results.

| Scenario | Action | Expected behavior |
|---|---|---|
| Android build | Configure restricted Maps key, build and launch | Purple MainView title, map tiles and controls |
| iOS build | Build/sign on Mac, launch iOS app | Same shared controls and native map |
| Consent denied | Deny permission at Start | Helpful denial message; no rows added |
| Capture | Grant permission and collect real GPS fixes | Count increases and colored cells appear |
| Density | Stay within one cell for several fixes, then move | Repeated cell warmer than isolated cells |
| Stop | Stop; wait 30 seconds | Count no longer increases after cancellation settles |
| Persistence | Stop, close app, reopen and Refresh | Saved heat cells still displayed |
| Lifecycle | Send app to background, return | Collection stops; Start required to resume |
| GPS disabled | Turn off device location services | Retry message, no invalid rows saved |
| No fix or poor accuracy | Test indoors or without a fix | Unavailable/poor fixes not persisted |
| Revoked permission | Revoke while tracking, then return | No unauthorized collection; informative status |
| Clear cancelled | Press Clear then Cancel | History unchanged |
| Clear confirmed | Press Clear then Delete | Count zero; heat overlay removed; zero after reopen |
| Display bounds | Add >5000 valid test rows in a separate test database | Only latest 5000 mapped, at most 900 circles |

Screenshot names: android-mainview.png, ios-mainview.png, heat-density.png, restart-persistence.png, clear-history.png, real-build-tests.png. Never substitute drawings or fabricated terminals. Do not submit private location data or secrets.
