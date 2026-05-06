# 导入GBL

## 1、Package Manager安装
- Add package from git URL: 
```
https://github.com/ousttrue/UniGLTF.git?path=/Assets/UniGLTF
```

## 2、安装后菜单栏多出的菜单
![安装](glb.png)


# 导入流程

## 1、点击import
![导入](import.png)


## 2、选择glb文件
![选择glb文件](select-glb.png)

## 3、选择导入目录和修改prefab名称
- 目录：client\Assets\Resources\Prefabs
![导入目录文件](target-prefab.png)

- 导入后增加3个文件夹和一个prefab
![导入后](prefab.png)


## 4、修改shader,使用URP
![shader](shader.png)

- 选择Simple Lit
![SimpleLit](SimpleLit.png)


## 5、拖拽材质
![metarial](metarial.png)


## 6、修改prefab，以便可以正常使用
- 添加GameObject, 命令Scale，用于缩放调整角度等
- 添加GameObject，命令Camp，用于阵营颜色区分
![adjust-prefab](adjust-prefab.png)

- 调整角度
![adjust-rotation](adjust-rotation.png)