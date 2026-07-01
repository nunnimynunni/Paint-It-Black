using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Corre desde: Paint-It-Black → Renombrar Jerarquía (PascalCase)
// Renombra los GameObjects con nombres inconsistentes a PascalCase.
// Los árboles/arbustos (arbol1_0, arbbbb_0) se dejan igual porque son
// nombres del tileset y no vale la pena tocarlos.
public class RenombrarJerarquia
{
    [MenuItem("Paint-It-Black/Renombrar Jerarquía (PascalCase)")]
    static void Renombrar()
    {
        var renombres = new Dictionary<string, string>
        {
            // Managers
            { "ammoManager",            "AmmoManager"           },
            { "dialogmanagerGomez",     "DialogManagerGomez"    },
            { "DialogManagerCromagus",  "DialogManagerCromagus" }, // ya ok, por si acaso
            { "entryStart",             "EntryStart"            },

            // Personajes
            { "frottnguy_0",            "Forastero"             },
            { "cromagustin",            "Cromagustin"           },
            { "gomez1",                 "Gomez"                 },

            // Mapa - Fondo
            { "pixellab-the-green-of-the-floor-is-a-li-1782327036156_0", "Piso" },
            { "eefsdfsdf_0",            "CollideresBorde"       },

            // Mapa - Estructuras
            { "cascada (2)_0",          "Cascada"               },
            { "casa_0 (1)",             "Casa1"                 },
            { "casa (1)_0",             "Casa2"                 },
            { "casa (2)_0",             "Casa3"                 },

            // Mapa - Collideres
            { "coso_0",                 "ZonaColisiones1"       },
            { "coso_0 (1)",             "ZonaColisiones2"       },
            { "collider rio",           "ColliderRio1"          },
            { "collider rio (1)",       "ColliderRio2"          },
            { "untitled (4)_0",         "ColliderPared1"        },
            { "untitled (5)_0",         "ColliderPared2"        },
            { "untitled (6)_0",         "ColliderPared3"        },

            // UI - HUD
            { "hudCombate",             "HudCombate"            },
            { "hudExploracion",         "HudExploracion"        },
            { "ammoHud",                "HudMunicion"           },
            { "vidaHud",                "HudVida"               },
            { "marcoVida",              "MarcoVida"             },
            { "marcoArmas",             "MarcoArmas"            },
            { "weaponhud",              "HudArmas"              },
            { "armaIcon",               "IconoArma"             },
            { "gota1",                  "Gota1"                 },
            { "gota2",                  "Gota2"                 },

            // UI - Paneles y diálogos
            { "dialogpanelGomez",       "DialogPanelGomez"      },
            { "dialogpanelCromagus",    "DialogPanelCromagus"   },
            { "dialogtext",             "DialogText"            },
            { "outline",                "Outline"               },
        };

        int count = 0;
        // Buscar en TODOS los objetos de la escena (no solo raíz)
        var todos = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var go in todos)
        {
            if (renombres.TryGetValue(go.name, out string nuevoNombre))
            {
                go.name = nuevoNombre;
                count++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"✅ {count} objetos renombrados. Guardá con Ctrl+S.");
    }
}
