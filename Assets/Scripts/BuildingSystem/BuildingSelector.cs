using System.Collections;
using System.Collections.Generic;
using GameInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuildingSystem {
    //选中需要建造的物品
    public class BuildingSelector : MonoBehaviour {
        [SerializeField]
        private List<TileClass> _buildables;//建造物列表
        [SerializeField]
        private BuildingPlacer _buildingPlacer;
        private int _activeBuildableIndex;

        private void OnEnable() {
            InputActions.Instance.World.NextItem.performed += OnNextItemPerformed;
        }

        private void OnNextItemPerformed(InputAction.CallbackContext ctx) {
            NextItem();
        }

        private void NextItem() {
            Debug.Log("下一个物品");
            _activeBuildableIndex = (_activeBuildableIndex + 1) % _buildables.Count;
            _buildingPlacer.SetActionBuildable(_buildables[_activeBuildableIndex]);
        }
    }
}

