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

        [SerializeField]
        private TextMesh DefenceText;

        public void UpdateStates(float healValue, float attackValue, float defenceValue)
        {
            healText.text = healValue.ToString();
            attackText.text = attackValue.ToString();
            DefenceText.text = defenceValue.ToString();
        }
    }
}