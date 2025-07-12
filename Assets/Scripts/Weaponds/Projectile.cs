using UnityEngine;
using Mirror;
using System.Linq;


public class ProjectileBullet : NetworkBehaviour
{
    public int damage = 10;
    public float lifeTime = 5f;
    public GameObject[] bloodImpactPrefabs;
    public GameObject[] metalImpactPrefabs;
    public GameObject[] dirtImpactPrefabs;
    private Rigidbody rb;
	public float projectileImpulse = 80f;
	public float zombieFollowTime = 10f;
	public Collider bulletCollider;
	

	[SyncVar] private NetworkIdentity shooterIdentity; // Track the shooter
    public float probabilityToDismember;
    public GameObject dismemberEffect;
	public bool canDestroy = false;

    public void SetShooter(NetworkIdentity shooter)
    {
        shooterIdentity = shooter;
    }
	public void SetDamage(int amount)
    {
		damage = amount;
    }
	


    public override void OnStartServer()
	{
		rb = GetComponent<Rigidbody>();
		rb.linearVelocity = transform.forward * projectileImpulse;

		// Ensure authority is transferred to the player who shot the bullet
		NetworkIdentity netIdentity = GetComponent<NetworkIdentity>();
		// if (netIdentity != null && netIdentity.connectionToClient != null)
		// {
		// 	netIdentity.AssignClientAuthority(netIdentity.connectionToClient);
		// }

		Destroy(gameObject, lifeTime);
	}
    void Update()
    {
        if (isServer && canDestroy) NetworkServer.Destroy(gameObject);
    }
    [Server]
	void OnCollisionEnter(Collision collision)
	{
		NetworkIdentity identity = collision.gameObject.GetComponentInParent<NetworkIdentity>();
		if (identity == null || identity == shooterIdentity) return;

		Vector3 hitPosition = collision.contacts[0].point;
		Debug.Log("is server that calls" + collision.collider.tag);

		// Notify all clients about the hit (for effects, animations, etc.)
		//RpcHit(identity, hitPosition);

		string[] dismemberTags = {
			"dismemberArmRight", "dismemberArmLeft",
			"dismemberLowerArmRight", "dismemberLowerArmLeft"
		};

		float rand = Random.Range(0f, 1f);

		// Limb dismember logic
		if (rand < probabilityToDismember && dismemberTags.Contains(collision.collider.tag))
		{
			Debug.Log("dismember" );
			// Send RPC to clients to hide that limb
			
			 // Then, send an RPC to update the clients
        	RpcHit(identity, hitPosition, collision.collider.tag,shooterIdentity);

			// Spawn dismember visual effect
			GameObject dismember = Instantiate(dismemberEffect, hitPosition, Quaternion.identity);
			NetworkServer.Spawn(dismember);
		}
		// Head dismember logic
		else if (rand < probabilityToDismember && collision.collider.CompareTag("dismemberHead"))
		{
			IDamageble damageable = collision.gameObject.GetComponentInParent<IDamageble>();
			damageable?.Damage(9999); // Just in case it's null

			
			 // Then, send an RPC to update the clients
        	RpcHit(identity, hitPosition, collision.collider.tag,shooterIdentity);

			GameObject dismember = Instantiate(dismemberEffect, hitPosition, Quaternion.identity);
			NetworkServer.Spawn(dismember);
		} else{
			RpcHit(identity, hitPosition, null, shooterIdentity);
		}
	}



		public GameObject FindChildWithTag(Transform parent, string tag)
		{
			foreach (Transform child in parent)
			{
				if (child.CompareTag(tag))
				{
					return child.gameObject;
				}

				// Recursive search through grandchildren
				GameObject result = FindChildWithTag(child, tag);
				if (result != null)
					return result;
			}

			return null;
		}

		

		[ClientRpc]
		void RpcHit(NetworkIdentity targetIdentity, Vector3 hitPosition, string tag, NetworkIdentity shooterIdent)
		{
			GameObject hitObject = null;
			if(targetIdentity != null){

				hitObject = targetIdentity.gameObject;
			}
			if( hitObject != null && hitPosition != null){

				Debug.Log("hit: " + hitObject);
				Debug.Log("hit: " + hitObject.tag);

				if (hitObject.CompareTag("Zombie") || hitObject.CompareTag("Player")) 
				{
					Instantiate(bloodImpactPrefabs[Random.Range(0, bloodImpactPrefabs.Length)], 
								hitPosition, 
								Quaternion.identity);

					IDamageble damageable = hitObject.GetComponentInParent<IDamageble>();
					if (damageable != null && shooterIdent != null && targetIdentity != null) 
					{
						damageable.Damage(damage, targetIdentity, shooterIdent);
					}
					
					ZombieAI zombie = hitObject.GetComponentInParent<ZombieAI>();
					if(zombie != null){
						zombie.followTime = zombieFollowTime;
						zombie.closestPlayer = shooterIdent.gameObject;
						zombie.StartFollowingPlayer();
					}
					if(tag != null){
						Debug.Log("dismember: call");
						CmdDismember(targetIdentity, tag);
					} else{
						canDestroy = true;
					}
					if (isServer) bulletCollider.enabled = false;
				}
			}

		}
		[Command(requiresAuthority = false)]
		void CmdDismember(NetworkIdentity targetIdentity, string tag){
			Debug.Log("dismember: cmd");
			RpcDismember(targetIdentity, tag);
		}

		[ClientRpc]
		void RpcDismember(NetworkIdentity targetIdentity, string tag)
		{
			Debug.Log("RpcDismember called");
			if (targetIdentity == null)
			{
				Debug.LogWarning("RpcDismember: targetIdentity is null!");
				return;
			}

			Debug.Log("RpcDismember: tag = " + tag);
			GameObject dismemberPart = FindChildWithTag(targetIdentity.transform, tag);
			if (dismemberPart != null)
			{
				dismemberPart.transform.localScale = Vector3.zero;
			}
			else
			{
				Debug.LogWarning("RpcDismember: Could not find child with tag: " + tag);
			}
			canDestroy = true;
		}


	
}
