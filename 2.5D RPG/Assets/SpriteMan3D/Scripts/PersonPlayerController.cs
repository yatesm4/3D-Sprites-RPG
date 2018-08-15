﻿using UnityEngine;

namespace SpriteMan3D
{
    /// <summary>
    /// A simple Input controller for detecting player actions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class PersonPlayerController : PersonController
    {
        /// <summary>
        /// A character's walking speed.
        /// </summary>
        public float walkSpeed = 2f;
        /// <summary>
        /// A character's running speed.
        /// </summary>
        public float runSpeed = 3f;
        /// <summary>
        /// A character's jump velocity.
        /// </summary>
        /// <remarks>
        /// Increase your project's Physics gravity and increase this value to make a character jump quickly.
        /// </remarks>
        public float jumpVelocity;

        /// <summary>
        /// How far to look for the distance to ground.
        /// </summary>
        public float groundDistanceOffset = 0.2f;

        /// <summary>
        /// The collider used for a mellee attack.
        /// </summary>
        public Collider attackCollider;

        /// <summary>
        /// Determines if this character can move.
        /// </summary>
        public bool canMove = true;
        /// <summary>
        /// Determines if this character can jump.
        /// </summary>
        public bool canJump = true;
        /// <summary>
        /// Determines if this character can attack.
        /// </summary>
        public bool canAttack = true;

        private Rigidbody rb;
        private float distToGround;
        private Collider charCollider;

        public float attackCooldown = 0.2f;
        private float attackTimer = 0f;

        void Start()
        {
            rb = transform.GetComponent<Rigidbody>();

            // get the distance to ground
            charCollider = GetComponent<Collider>();
            distToGround = charCollider.bounds.extents.y;
        }

        Vector3 offset;

        void Update()
        {
            IsGrounded = Physics.Raycast(transform.position, -Vector3.up, distToGround + groundDistanceOffset);

            HandleAttack();
            HandleJump();
        }

        void FixedUpdate()
        {
            HandleMove();
        }

        void HandleMove()
        {
            if (canMove)
            {
                var speed = walkSpeed;

                // detect input movement
                var moveHorizontal = Input.GetAxis("Horizontal");
                var moveVertical = Input.GetAxis("Vertical");
                IsMoving = moveHorizontal != 0 || moveVertical != 0;

                IsRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if(IsRunning)
                {
                    speed = runSpeed;
                }

                // rotate the character
                var movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
                var rot = movement * (speed / 10);

                if (attackTimer <= 0 && movement != Vector3.zero)
                {
                    var newRotation = Quaternion.LookRotation(rot);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation, 10f);
                }

                // move the character
                if (IsMoving && offset.y != 0f)
                {
                    movement.y = offset.normalized.y * movement.magnitude;
                }
                movement *= (speed / 10);

                var characterMovement = transform.position + movement;
                if (attackTimer <= 0 || !IsGrounded)
                {
                    rb.MovePosition(characterMovement);
                }
            }
        }

        private void HandleJump()
        {
            if (canJump)
            {
                // detect jump
                JumpStarted = Input.GetButtonDown("Jump");

                // make the character jump
                if (JumpStarted && IsGrounded)
                {
                    var velocity = rb.velocity;
                    velocity.y = jumpVelocity;
                    rb.velocity = velocity;
                }
            }
        }

        private void HandleAttack()
        {
            if (canAttack)
            {
                if (attackTimer <= 0)
                {
                    // detect attack
                    AttackStarted = Input.GetButtonDown("Fire1");
                    if (AttackStarted)
                    {
                        attackTimer = attackCooldown;
                    }
                }
                else
                {
                    if(AttackStarted)
                    {
                        AttackStarted = false;
                    }
                    attackTimer -= Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// called as animation event from Attack animation.
        /// </summary>
        public void StartAttack()
        {
            attackCollider.enabled = true;
        }

        /// <summary>
        /// called as animation event from Attack animation.
        /// </summary>
        public void EndAttack()
        {
            attackCollider.enabled = false;
        }
    }
}
