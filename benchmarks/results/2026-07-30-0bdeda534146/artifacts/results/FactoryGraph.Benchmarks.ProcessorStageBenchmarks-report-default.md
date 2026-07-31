
BenchmarkDotNet v0.15.8, Linux CachyOS
AMD Ryzen 5 5600G with Radeon Graphics 4.13GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.110
  [Host]   : .NET 10.0.10 (10.0.10, 42.42.42.42424), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.10 (10.0.10, 42.42.42.42424), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

 Method             | Family           | Mode           | MachineGroups | Mean         | Error       | StdDev     | Gen0    | Gen1    | Allocated |
------------------- |----------------- |--------------- |-------------- |-------------:|------------:|-----------:|--------:|--------:|----------:|
 **EndToEnd**           | **AcyclicChain**     | **TargetDriven**   | **8**             |   **514.289 μs** | **134.3383 μs** |  **7.3635 μs** |  **5.8594** |       **-** |  **53.64 KB** |
 **EndToEnd**           | **AcyclicChain**     | **TargetDriven**   | **32**            |   **680.474 μs** | **211.0321 μs** | **11.5674 μs** | **21.4844** |  **0.9766** | **177.99 KB** |
 **EndToEnd**           | **AcyclicChain**     | **TargetDriven**   | **128**           | **1,372.917 μs** | **844.8017 μs** | **46.3064 μs** | **83.9844** | **15.6250** | **692.34 KB** |
 **EndToEnd**           | **AcyclicChain**     | **CapacityDriven** | **8**             |   **537.756 μs** | **640.8845 μs** | **35.1290 μs** |  **6.8359** |       **-** |  **58.95 KB** |
 **EndToEnd**           | **AcyclicChain**     | **CapacityDriven** | **32**            |   **691.990 μs** | **141.8560 μs** |  **7.7756 μs** | **23.4375** |  **0.9766** | **193.58 KB** |
 **EndToEnd**           | **AcyclicChain**     | **CapacityDriven** | **128**           | **1,245.903 μs** | **256.3375 μs** | **14.0507 μs** | **91.7969** | **17.5781** | **752.53 KB** |
 **EndToEnd**           | **ConstrainedChain** | **TargetDriven**   | **8**             |   **485.903 μs** |  **74.8237 μs** |  **4.1013 μs** |  **5.8594** |       **-** |   **54.9 KB** |
 **EndToEnd**           | **ConstrainedChain** | **TargetDriven**   | **32**            |   **664.499 μs** |  **14.2939 μs** |  **0.7835 μs** | **21.4844** |  **0.9766** | **182.63 KB** |
 **EndToEnd**           | **ConstrainedChain** | **TargetDriven**   | **128**           | **1,407.978 μs** |  **32.9425 μs** |  **1.8057 μs** | **85.9375** | **19.5313** | **710.48 KB** |
 **EndToEnd**           | **ConstrainedChain** | **CapacityDriven** | **8**             |   **493.414 μs** |  **52.2567 μs** |  **2.8644 μs** |  **6.8359** |       **-** |  **60.04 KB** |
 **EndToEnd**           | **ConstrainedChain** | **CapacityDriven** | **32**            |   **696.432 μs** | **347.1758 μs** | **19.0299 μs** | **23.4375** |  **0.9766** | **198.04 KB** |
 **EndToEnd**           | **ConstrainedChain** | **CapacityDriven** | **128**           | **1,368.006 μs** | **165.5195 μs** |  **9.0727 μs** | **93.7500** | **17.5781** | **770.46 KB** |
 **EndToEnd**           | **RecyclingRing**    | **TargetDriven**   | **8**             |   **516.938 μs** | **135.2825 μs** |  **7.4153 μs** |  **6.8359** |       **-** |  **56.89 KB** |
 **EndToEnd**           | **RecyclingRing**    | **TargetDriven**   | **32**            |   **619.700 μs** |  **38.4428 μs** |  **2.1072 μs** | **21.4844** |  **0.9766** | **179.68 KB** |
 **EndToEnd**           | **RecyclingRing**    | **TargetDriven**   | **128**           | **1,294.441 μs** | **373.3359 μs** | **20.4638 μs** | **83.9844** | **15.6250** | **691.64 KB** |
 **EndToEnd**           | **RecyclingRing**    | **CapacityDriven** | **8**             |   **464.529 μs** |  **94.1633 μs** |  **5.1614 μs** |  **6.8359** |       **-** |  **62.41 KB** |
 **EndToEnd**           | **RecyclingRing**    | **CapacityDriven** | **32**            |   **631.071 μs** |  **46.3793 μs** |  **2.5422 μs** | **23.4375** |  **0.9766** | **195.42 KB** |
 **EndToEnd**           | **RecyclingRing**    | **CapacityDriven** | **128**           | **1,367.576 μs** | **764.2830 μs** | **41.8929 μs** | **91.7969** | **17.5781** | **751.66 KB** |
 **BackendSolve**       | **AcyclicChain**     | **TargetDriven**   | **8**             |   **281.231 μs** |  **18.2042 μs** |  **0.9978 μs** |       **-** |       **-** |   **2.45 KB** |
 **BackendSolve**       | **AcyclicChain**     | **TargetDriven**   | **32**            |   **326.026 μs** |  **31.6118 μs** |  **1.7327 μs** |  **0.9766** |       **-** |   **8.64 KB** |
 **BackendSolve**       | **AcyclicChain**     | **TargetDriven**   | **128**           |   **530.858 μs** |  **54.0398 μs** |  **2.9621 μs** |  **3.9063** |       **-** |   **33.4 KB** |
 **BackendSolve**       | **AcyclicChain**     | **CapacityDriven** | **8**             |   **277.020 μs** |  **22.9512 μs** |  **1.2580 μs** |       **-** |       **-** |   **2.58 KB** |
 **BackendSolve**       | **AcyclicChain**     | **CapacityDriven** | **32**            |   **328.139 μs** |  **12.4057 μs** |  **0.6800 μs** |  **0.9766** |       **-** |   **8.77 KB** |
 **BackendSolve**       | **AcyclicChain**     | **CapacityDriven** | **128**           |   **540.045 μs** | **105.6728 μs** |  **5.7923 μs** |  **3.9063** |       **-** |  **33.52 KB** |
 **BackendSolve**       | **ConstrainedChain** | **TargetDriven**   | **8**             |   **280.243 μs** |  **40.7971 μs** |  **2.2362 μs** |       **-** |       **-** |   **2.45 KB** |
 **BackendSolve**       | **ConstrainedChain** | **TargetDriven**   | **32**            |   **324.687 μs** |  **36.4505 μs** |  **1.9980 μs** |  **0.9766** |       **-** |   **8.64 KB** |
 **BackendSolve**       | **ConstrainedChain** | **TargetDriven**   | **128**           |   **541.907 μs** |  **18.9227 μs** |  **1.0372 μs** |  **3.9063** |       **-** |   **33.4 KB** |
 **BackendSolve**       | **ConstrainedChain** | **CapacityDriven** | **8**             |   **283.958 μs** |  **41.6603 μs** |  **2.2835 μs** |       **-** |       **-** |   **2.58 KB** |
 **BackendSolve**       | **ConstrainedChain** | **CapacityDriven** | **32**            |   **332.898 μs** |  **23.4106 μs** |  **1.2832 μs** |  **0.9766** |       **-** |   **8.77 KB** |
 **BackendSolve**       | **ConstrainedChain** | **CapacityDriven** | **128**           |   **546.012 μs** |  **54.2156 μs** |  **2.9717 μs** |  **3.9063** |       **-** |  **33.52 KB** |
 **BackendSolve**       | **RecyclingRing**    | **TargetDriven**   | **8**             |   **279.204 μs** |  **26.7797 μs** |  **1.4679 μs** |       **-** |       **-** |   **2.59 KB** |
 **BackendSolve**       | **RecyclingRing**    | **TargetDriven**   | **32**            |   **337.105 μs** |  **95.0068 μs** |  **5.2076 μs** |  **0.9766** |       **-** |   **8.78 KB** |
 **BackendSolve**       | **RecyclingRing**    | **TargetDriven**   | **128**           |   **567.670 μs** |  **67.6758 μs** |  **3.7095 μs** |  **3.9063** |       **-** |  **33.54 KB** |
 **BackendSolve**       | **RecyclingRing**    | **CapacityDriven** | **8**             |   **287.846 μs** |  **12.3464 μs** |  **0.6768 μs** |       **-** |       **-** |   **2.73 KB** |
 **BackendSolve**       | **RecyclingRing**    | **CapacityDriven** | **32**            |   **350.987 μs** |  **50.4212 μs** |  **2.7638 μs** |  **0.9766** |       **-** |   **8.92 KB** |
 **BackendSolve**       | **RecyclingRing**    | **CapacityDriven** | **128**           |   **583.225 μs** | **161.3365 μs** |  **8.8434 μs** |  **3.9063** |       **-** |  **33.67 KB** |
 **ModelConstruction**  | **AcyclicChain**     | **TargetDriven**   | **8**             |     **4.777 μs** |   **2.0669 μs** |  **0.1133 μs** |  **1.6785** |  **0.0305** |  **13.76 KB** |
 **ModelConstruction**  | **AcyclicChain**     | **TargetDriven**   | **32**            |    **16.434 μs** |   **8.1892 μs** |  **0.4489 μs** |  **5.9509** |  **0.3357** |  **48.83 KB** |
 **ModelConstruction**  | **AcyclicChain**     | **TargetDriven**   | **128**           |    **65.149 μs** |  **33.4075 μs** |  **1.8312 μs** | **22.8271** |  **4.6387** | **186.88 KB** |
 **ModelConstruction**  | **AcyclicChain**     | **CapacityDriven** | **8**             |     **4.814 μs** |   **1.0668 μs** |  **0.0585 μs** |  **1.7624** |  **0.0305** |   **14.4 KB** |
 **ModelConstruction**  | **AcyclicChain**     | **CapacityDriven** | **32**            |    **17.291 μs** |   **5.5754 μs** |  **0.3056 μs** |  **6.1340** |  **0.3357** |  **50.22 KB** |
 **ModelConstruction**  | **AcyclicChain**     | **CapacityDriven** | **128**           |    **67.276 μs** |  **26.2772 μs** |  **1.4403 μs** | **23.3154** |  **4.7607** | **191.24 KB** |
 **ModelConstruction**  | **ConstrainedChain** | **TargetDriven**   | **8**             |     **5.139 μs** |   **3.1884 μs** |  **0.1748 μs** |  **1.6861** |  **0.0305** |  **13.79 KB** |
 **ModelConstruction**  | **ConstrainedChain** | **TargetDriven**   | **32**            |    **17.039 μs** |   **2.5459 μs** |  **0.1395 μs** |  **5.9814** |  **0.3357** |  **48.86 KB** |
 **ModelConstruction**  | **ConstrainedChain** | **TargetDriven**   | **128**           |    **68.756 μs** |  **37.5165 μs** |  **2.0564 μs** | **22.8271** |  **4.6387** | **186.88 KB** |
 **ModelConstruction**  | **ConstrainedChain** | **CapacityDriven** | **8**             |     **5.004 μs** |   **1.9569 μs** |  **0.1073 μs** |  **1.7624** |  **0.0305** |   **14.4 KB** |
 **ModelConstruction**  | **ConstrainedChain** | **CapacityDriven** | **32**            |    **18.290 μs** |  **10.4561 μs** |  **0.5731 μs** |  **6.1340** |  **0.3357** |  **50.22 KB** |
 **ModelConstruction**  | **ConstrainedChain** | **CapacityDriven** | **128**           |    **70.407 μs** |  **27.7989 μs** |  **1.5238 μs** | **23.3154** |  **4.7607** | **191.24 KB** |
 **ModelConstruction**  | **RecyclingRing**    | **TargetDriven**   | **8**             |     **4.934 μs** |   **0.8227 μs** |  **0.0451 μs** |  **1.7395** |  **0.0305** |  **14.21 KB** |
 **ModelConstruction**  | **RecyclingRing**    | **TargetDriven**   | **32**            |    **16.944 μs** |   **9.4464 μs** |  **0.5178 μs** |  **6.0120** |  **0.3662** |  **49.16 KB** |
 **ModelConstruction**  | **RecyclingRing**    | **TargetDriven**   | **128**           |    **67.547 μs** |  **30.0789 μs** |  **1.6487 μs** | **22.8271** |  **4.3945** | **187.18 KB** |
 **ModelConstruction**  | **RecyclingRing**    | **CapacityDriven** | **8**             |     **5.120 μs** |   **2.2021 μs** |  **0.1207 μs** |  **1.8158** |  **0.0305** |  **14.84 KB** |
 **ModelConstruction**  | **RecyclingRing**    | **CapacityDriven** | **32**            |    **18.364 μs** |   **7.3498 μs** |  **0.4029 μs** |  **6.1646** |  **0.3662** |  **50.53 KB** |
 **ModelConstruction**  | **RecyclingRing**    | **CapacityDriven** | **128**           |    **71.072 μs** |  **22.2254 μs** |  **1.2182 μs** | **23.4375** |  **4.6387** | **191.55 KB** |
 **ResultConstruction** | **AcyclicChain**     | **TargetDriven**   | **8**             |     **2.732 μs** |   **2.8121 μs** |  **0.1541 μs** |  **1.2474** |  **0.0114** |  **10.21 KB** |
 **ResultConstruction** | **AcyclicChain**     | **TargetDriven**   | **32**            |     **9.042 μs** |   **5.8317 μs** |  **0.3197 μs** |  **4.4250** |  **0.1221** |  **36.23 KB** |
 **ResultConstruction** | **AcyclicChain**     | **TargetDriven**   | **128**           |    **35.588 μs** |  **13.8364 μs** |  **0.7584 μs** | **17.0898** |  **1.7090** | **140.05 KB** |
 **ResultConstruction** | **AcyclicChain**     | **CapacityDriven** | **8**             |     **4.419 μs** |   **0.9954 μs** |  **0.0546 μs** |  **1.8158** |  **0.0229** |  **14.85 KB** |
 **ResultConstruction** | **AcyclicChain**     | **CapacityDriven** | **32**            |    **13.515 μs** |   **0.8237 μs** |  **0.0451 μs** |  **6.1646** |  **0.2289** |   **50.4 KB** |
 **ResultConstruction** | **AcyclicChain**     | **CapacityDriven** | **128**           |    **54.115 μs** |  **45.7787 μs** |  **2.5093 μs** | **23.9258** |  **3.1128** | **195.49 KB** |
 **ResultConstruction** | **ConstrainedChain** | **TargetDriven**   | **8**             |     **2.786 μs** |   **1.8671 μs** |  **0.1023 μs** |  **1.2474** |  **0.0114** |  **10.21 KB** |
 **ResultConstruction** | **ConstrainedChain** | **TargetDriven**   | **32**            |     **9.142 μs** |   **5.3229 μs** |  **0.2918 μs** |  **4.4250** |  **0.1221** |  **36.23 KB** |
 **ResultConstruction** | **ConstrainedChain** | **TargetDriven**   | **128**           |    **36.353 μs** |  **24.1143 μs** |  **1.3218 μs** | **17.0898** |  **1.7090** | **140.05 KB** |
 **ResultConstruction** | **ConstrainedChain** | **CapacityDriven** | **8**             |     **4.399 μs** |   **2.8635 μs** |  **0.1570 μs** |  **1.7929** |  **0.0229** |  **14.67 KB** |
 **ResultConstruction** | **ConstrainedChain** | **CapacityDriven** | **32**            |    **14.167 μs** |   **6.4495 μs** |  **0.3535 μs** |  **6.1340** |  **0.2289** |  **50.22 KB** |
 **ResultConstruction** | **ConstrainedChain** | **CapacityDriven** | **128**           |    **55.728 μs** |  **25.5394 μs** |  **1.3999 μs** | **23.8647** |  **3.3569** | **195.31 KB** |
 **ResultConstruction** | **RecyclingRing**    | **TargetDriven**   | **8**             |     **2.917 μs** |   **0.9134 μs** |  **0.0501 μs** |  **1.2665** |  **0.0114** |  **10.37 KB** |
 **ResultConstruction** | **RecyclingRing**    | **TargetDriven**   | **32**            |    **11.269 μs** |  **22.1437 μs** |  **1.2138 μs** |  **4.4403** |  **0.1221** |  **36.39 KB** |
 **ResultConstruction** | **RecyclingRing**    | **TargetDriven**   | **128**           |    **38.006 μs** |  **19.1087 μs** |  **1.0474 μs** | **17.1509** |  **1.7090** | **140.21 KB** |
 **ResultConstruction** | **RecyclingRing**    | **CapacityDriven** | **8**             |     **4.577 μs** |   **2.9146 μs** |  **0.1598 μs** |  **1.8463** |  **0.0229** |  **15.13 KB** |
 **ResultConstruction** | **RecyclingRing**    | **CapacityDriven** | **32**            |    **14.794 μs** |  **12.1851 μs** |  **0.6679 μs** |  **6.1951** |  **0.2441** |  **50.67 KB** |
 **ResultConstruction** | **RecyclingRing**    | **CapacityDriven** | **128**           |    **56.440 μs** |  **39.5164 μs** |  **2.1660 μs** | **23.9258** |  **3.2959** | **195.77 KB** |
 **TopologyAnalysis**   | **AcyclicChain**     | **TargetDriven**   | **8**             |     **4.337 μs** |   **0.9010 μs** |  **0.0494 μs** |  **1.4191** |  **0.0076** |  **11.63 KB** |
 **TopologyAnalysis**   | **AcyclicChain**     | **TargetDriven**   | **32**            |    **15.228 μs** |   **3.3566 μs** |  **0.1840 μs** |  **4.6844** |  **0.1373** |  **38.38 KB** |
 **TopologyAnalysis**   | **AcyclicChain**     | **TargetDriven**   | **128**           |    **60.471 μs** |  **27.3294 μs** |  **1.4980 μs** | **18.9209** |  **2.0752** | **155.27 KB** |
 **TopologyAnalysis**   | **AcyclicChain**     | **CapacityDriven** | **8**             |     **4.559 μs** |   **6.7061 μs** |  **0.3676 μs** |  **1.4191** |  **0.0076** |  **11.63 KB** |
 **TopologyAnalysis**   | **AcyclicChain**     | **CapacityDriven** | **32**            |    **15.315 μs** |  **10.4054 μs** |  **0.5704 μs** |  **4.6844** |  **0.1373** |  **38.38 KB** |
 **TopologyAnalysis**   | **AcyclicChain**     | **CapacityDriven** | **128**           |    **59.836 μs** |  **17.3810 μs** |  **0.9527 μs** | **18.9819** |  **2.1362** | **155.27 KB** |
 **TopologyAnalysis**   | **ConstrainedChain** | **TargetDriven**   | **8**             |     **4.017 μs** |   **1.7114 μs** |  **0.0938 μs** |  **1.4191** |  **0.0076** |  **11.63 KB** |
 **TopologyAnalysis**   | **ConstrainedChain** | **TargetDriven**   | **32**            |    **15.163 μs** |   **4.0038 μs** |  **0.2195 μs** |  **4.6844** |  **0.1373** |  **38.38 KB** |
 **TopologyAnalysis**   | **ConstrainedChain** | **TargetDriven**   | **128**           |    **59.840 μs** |  **25.1781 μs** |  **1.3801 μs** | **18.9819** |  **2.1362** | **155.27 KB** |
 **TopologyAnalysis**   | **ConstrainedChain** | **CapacityDriven** | **8**             |     **4.431 μs** |   **3.4897 μs** |  **0.1913 μs** |  **1.4191** |  **0.0076** |  **11.63 KB** |
 **TopologyAnalysis**   | **ConstrainedChain** | **CapacityDriven** | **32**            |    **14.022 μs** |   **6.2847 μs** |  **0.3445 μs** |  **4.6844** |  **0.1373** |  **38.38 KB** |
 **TopologyAnalysis**   | **ConstrainedChain** | **CapacityDriven** | **128**           |    **57.036 μs** |  **28.6182 μs** |  **1.5687 μs** | **18.9819** |  **2.1362** | **155.27 KB** |
 **TopologyAnalysis**   | **RecyclingRing**    | **TargetDriven**   | **8**             |     **5.082 μs** |   **0.5648 μs** |  **0.0310 μs** |  **1.6556** |  **0.0229** |  **13.52 KB** |
 **TopologyAnalysis**   | **RecyclingRing**    | **TargetDriven**   | **32**            |    **14.864 μs** |   **6.1324 μs** |  **0.3361 μs** |  **4.7455** |  **0.1526** |  **38.82 KB** |
 **TopologyAnalysis**   | **RecyclingRing**    | **TargetDriven**   | **128**           |    **60.868 μs** |  **19.8171 μs** |  **1.0862 μs** | **18.7378** |  **2.1973** |  **153.1 KB** |
 **TopologyAnalysis**   | **RecyclingRing**    | **CapacityDriven** | **8**             |     **5.195 μs** |   **2.5626 μs** |  **0.1405 μs** |  **1.6556** |  **0.0229** |  **13.52 KB** |
 **TopologyAnalysis**   | **RecyclingRing**    | **CapacityDriven** | **32**            |    **16.345 μs** |   **8.8802 μs** |  **0.4868 μs** |  **4.7302** |  **0.1221** |  **38.82 KB** |
 **TopologyAnalysis**   | **RecyclingRing**    | **CapacityDriven** | **128**           |    **61.330 μs** |   **2.7910 μs** |  **0.1530 μs** | **18.6768** |  **2.3193** | **153.04 KB** |
 **Validation**         | **AcyclicChain**     | **TargetDriven**   | **8**             |     **5.252 μs** |   **3.0531 μs** |  **0.1673 μs** |  **1.6861** |  **0.0076** |  **13.78 KB** |
 **Validation**         | **AcyclicChain**     | **TargetDriven**   | **32**            |    **16.433 μs** |   **6.6263 μs** |  **0.3632 μs** |  **5.0964** |  **0.0610** |  **41.67 KB** |
 **Validation**         | **AcyclicChain**     | **TargetDriven**   | **128**           |    **59.381 μs** |  **33.6338 μs** |  **1.8436 μs** | **19.8975** |  **1.1597** | **162.98 KB** |
 **Validation**         | **AcyclicChain**     | **CapacityDriven** | **8**             |     **5.208 μs** |   **2.2143 μs** |  **0.1214 μs** |  **1.7014** |  **0.0076** |  **13.91 KB** |
 **Validation**         | **AcyclicChain**     | **CapacityDriven** | **32**            |    **16.681 μs** |   **3.4868 μs** |  **0.1911 μs** |  **5.0964** |  **0.0305** |  **41.77 KB** |
 **Validation**         | **AcyclicChain**     | **CapacityDriven** | **128**           |    **62.931 μs** |  **22.5981 μs** |  **1.2387 μs** | **19.8975** |  **1.0986** | **163.04 KB** |
 **Validation**         | **ConstrainedChain** | **TargetDriven**   | **8**             |     **5.526 μs** |   **2.4077 μs** |  **0.1320 μs** |  **1.8387** |  **0.0076** |  **15.08 KB** |
 **Validation**         | **ConstrainedChain** | **TargetDriven**   | **32**            |    **17.395 μs** |   **8.6601 μs** |  **0.4747 μs** |  **5.6458** |  **0.0610** |  **46.28 KB** |
 **Validation**         | **ConstrainedChain** | **TargetDriven**   | **128**           |    **65.902 μs** |  **23.8081 μs** |  **1.3050 μs** | **22.0947** |  **1.2207** | **181.09 KB** |
 **Validation**         | **ConstrainedChain** | **CapacityDriven** | **8**             |     **5.647 μs** |   **2.7407 μs** |  **0.1502 μs** |  **1.8539** |  **0.0076** |  **15.17 KB** |
 **Validation**         | **ConstrainedChain** | **CapacityDriven** | **32**            |    **17.184 μs** |   **5.2743 μs** |  **0.2891 μs** |  **5.6763** |  **0.0610** |  **46.41 KB** |
 **Validation**         | **ConstrainedChain** | **CapacityDriven** | **128**           |    **64.616 μs** |  **40.2953 μs** |  **2.2087 μs** | **22.0947** |  **1.2207** | **181.21 KB** |
 **Validation**         | **RecyclingRing**    | **TargetDriven**   | **8**             |     **5.347 μs** |   **1.0986 μs** |  **0.0602 μs** |  **1.7700** |  **0.0076** |   **14.5 KB** |
 **Validation**         | **RecyclingRing**    | **TargetDriven**   | **32**            |    **16.285 μs** |   **7.4144 μs** |  **0.4064 μs** |  **5.1575** |  **0.0610** |  **42.22 KB** |
 **Validation**         | **RecyclingRing**    | **TargetDriven**   | **128**           |    **56.698 μs** |  **11.4414 μs** |  **0.6271 μs** | **20.0195** |  **1.0986** | **163.56 KB** |
 **Validation**         | **RecyclingRing**    | **CapacityDriven** | **8**             |     **5.113 μs** |   **2.7538 μs** |  **0.1509 μs** |  **1.7853** |  **0.0076** |  **14.59 KB** |
 **Validation**         | **RecyclingRing**    | **CapacityDriven** | **32**            |    **16.288 μs** |   **5.8310 μs** |  **0.3196 μs** |  **5.1575** |  **0.0610** |  **42.35 KB** |
 **Validation**         | **RecyclingRing**    | **CapacityDriven** | **128**           |    **56.871 μs** |   **4.7628 μs** |  **0.2611 μs** | **20.0195** |  **0.9766** | **163.65 KB** |
