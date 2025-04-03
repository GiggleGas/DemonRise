using PDR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public class EnemyPawn : MapPawn
    {
        public float _health;
        public float _maxHealth;
        public float _attack;
        public int _experience;

        public EnemyPawn(Vector2Int gridPosition, GameObject gameObject, float health, float maxHealth, float attack, int exp) : base(gridPosition, gameObject)
        {
             _health = health;
            _maxHealth = maxHealth;
            _attack = attack;
            _experience = exp;
        }
    }
}
