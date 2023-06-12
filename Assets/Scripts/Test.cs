using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;

public class Test : MonoBehaviour
{

    public AssetReferenceGameObject cellPrefabReference;
    HexCell[] cells;

    IEnumerator Start()
    {

        yield return new WaitForSeconds(1);

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        int cellCountZ = 20 * HexMetrics.chunkSizeZ, cellCountX = 20 * HexMetrics.chunkSizeX;
        cells = new HexCell[cellCountX * cellCountZ];
        AsyncOperationHandle<GameObject>[] asyncOperationHandles = new AsyncOperationHandle<GameObject>[cellCountX * cellCountZ];
        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                asyncOperationHandles[i] = Addressables.InstantiateAsync(cellPrefabReference);
                int _x = x, _z = z, _i = i;
                asyncOperationHandles[i].Completed += (asyncOperationHandle) =>
                {
                    HexCell cell = cells[_i] = asyncOperationHandle.Result.GetComponent<HexCell>();
                    cell.Index = _i;

                };
                i++;
            }
            // if (z % (HexMetrics.chunkSizeZ / 2) == 0)
            // {
            //     // LoadingScreen.Instance.UpdateLoading(i / ((float)cellCount));
            //     yield return new WaitForEndOfFrame();
            // }
        }

        yield return new WaitUntil(() => asyncOperationHandles.All(x => x.IsDone));
        sw.Stop();
        Debug.Log(string.Format("Instanciate in: {0}ms", sw.ElapsedMilliseconds));
    }
}
