using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Recycler.
/// </summary>
public class Recycler : MonoBehaviour {
	/// <summary>
	/// �Ƿ���ظ�����
	/// </summary>
	public bool isPooled = false;
	
	protected void Recycle () {
		Pool.Recycle (gameObject);
	}
}
