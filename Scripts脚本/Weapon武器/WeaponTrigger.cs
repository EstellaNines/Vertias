using UnityEngine;

public class WeaponTrigger : MonoBehaviour
{
    // �ӵ������λ��
    public Transform Muzzle;

    // �ӵ���
    public BulletPool bulletPool;

    // �Ƿ����������־
    private bool isFiring;

    // ������ʱ��
    public float ShootInterval;

    // ��ʱ�������ڿ������Ƶ��
    private float Timer;

    //ɢ���Ƕ��ֶ�
    public float spreadAngle = 5f;

    // �ⲿ�������״̬�Ľӿ�
    public void SetFiring(bool firing)
    {
        isFiring = firing;
    }

    // ÿ֡����һ��
    void Update()
    {
        // ��ʱ������
        Timer += Time.deltaTime;

        // �����������Ҽ�ʱ���ﵽ������
        if (isFiring && Timer >= ShootInterval)
        {
            // ���ü�ʱ��
            Timer = 0;

            // ִ�����
            Fire();
        }
    }

    // ����߼�
    private void Fire()
    {
        // �ӳ��л�ȡһ���ӵ�
        GameObject bulletObj = bulletPool.GetBullet();
        if (bulletObj == null) return;

        // �����ӵ�λ�ú���ת
        bulletObj.transform.position = Muzzle.position;
        bulletObj.transform.rotation = Muzzle.rotation;

        // �������ɢ��ƫ��
        float randomAngle = Random.Range(-spreadAngle, spreadAngle);
        bulletObj.transform.rotation = Muzzle.rotation * Quaternion.Euler(0, 0, randomAngle);

        // ��ȡ�ӵ����
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            // �����ӵ��ĳ�ʼλ��
            bullet.StartPos = Muzzle.position;

            // �����ӵ�������Ϊ���
            bullet.shooter = this.transform.parent; // �����ĸ�������ң�
            Debug.Log($"[��ҿ���] ������: {bullet.shooter.name}");

            // �����ӵ����˶�����
            Rigidbody2D rb = bulletObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = bulletObj.transform.right * bullet.BulletSpeed;
            }
        }

        // �����ӵ�
        bulletObj.SetActive(true);
    }
}