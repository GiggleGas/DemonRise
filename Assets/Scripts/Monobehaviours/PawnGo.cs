using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public class PawnGo : MonoBehaviour
    {
        [SerializeField]
        private TextMesh healText;

        [SerializeField]
        private TextMesh attackText;

        public void UpdateStates(float healValue, float attackValue)
        {
            healText.text = healValue.ToString();
            attackText.text = attackValue.ToString();
        }
    }
}