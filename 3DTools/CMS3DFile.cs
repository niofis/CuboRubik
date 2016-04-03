/* Copyright (c) 2007, niofis
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*     * Redistributions of source code must retain the above copyright
*       notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright
*       notice, this list of conditions and the following disclaimer in the
*       documentation and/or other materials provided with the distribution.
*
* THIS SOFTWARE IS PROVIDED BY niofis ``AS IS'' AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL niofis BE LIABLE FOR ANY
* DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
* (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
* ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
* SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/


using System;
using System.Collections;
using System.IO;

namespace _3DTools
{

    public class CMS3DFile
    {
	    private CMS3DFileI _i;
    	
	    private uint MAKEDWORD(ushort a, ushort b)
	    {
		    return ((uint)(((ushort)(a)) | ((ushort)((ushort)(b))) << 16));
	    }
    	
	    static void Swap(ref ushort x, ref ushort y)
	    {
	        ushort temp = x;
	        x = y;
	        y = temp;
	    }


	    public CMS3DFile()
	    {
		    _i = new CMS3DFileI();
	    }
    	
	    //virtual ~CMS3DFile();


	    public bool LoadFromFile(Stream file)
	    {
            Stream fs = file;
		    if (fs==null)
			    return false;
    	
		    BinaryReader br=new BinaryReader(fs);
    		
		    int i;
    		
		    ms3d_header_t header=new ms3d_header_t();
    		
		    header.Load(br);
    	
		    if (header.id.CompareTo("MS3D000000") != 0)
			    return false;
    	
		    if (header.version != 4)
			    return false;
    	
		    // vertices
		    ushort nNumVertices;
		    nNumVertices=br.ReadUInt16();
    		
    		
		    _i.arrVertices=new ms3d_vertex_t[nNumVertices];
		    for(i=0;i<nNumVertices;i++)
		    {
			    _i.arrVertices[i]=new ms3d_vertex_t();
			    _i.arrVertices[i].Load(br);
		    }
    	
		    // triangles
		    ushort nNumTriangles;
		    nNumTriangles=br.ReadUInt16();
		    _i.arrTriangles = new ms3d_triangle_t[nNumTriangles];
		    for(i=0;i<nNumTriangles;i++)
		    {
			    _i.arrTriangles[i]=new ms3d_triangle_t();
			    _i.arrTriangles[i].Load(br);
		    }
    	
		    // edges
		    ArrayList setEdgePair=new ArrayList();
		    for (i = 0; i < _i.arrTriangles.Length; i++)
		    {
			    ushort a, b;
			    a = _i.arrTriangles[i].vertexIndices[0];
			    b = _i.arrTriangles[i].vertexIndices[1];
			    if (a > b)
				    Swap(ref a, ref b);
			    if (setEdgePair.IndexOf(MAKEDWORD(a, b)) == -1)
				    setEdgePair.Add(MAKEDWORD(a, b));
    	
			    a = _i.arrTriangles[i].vertexIndices[1];
			    b = _i.arrTriangles[i].vertexIndices[2];
			    if (a > b)
				    Swap(ref a,ref b);
			    if (setEdgePair.IndexOf(MAKEDWORD(a, b)) == -1)
				    setEdgePair.Add(MAKEDWORD(a, b));
    	
			    a = _i.arrTriangles[i].vertexIndices[2];
			    b = _i.arrTriangles[i].vertexIndices[0];
			    if (a > b)
				    Swap(ref a,ref b);
			    if (setEdgePair.IndexOf(MAKEDWORD(a, b)) == -1)
				    setEdgePair.Add(MAKEDWORD(a, b));
		    }
    		
		    _i.arrEdges=new ms3d_edge_t[setEdgePair.Count];
		    for(i=0; i<setEdgePair.Count; i++)
		    {
			    _i.arrEdges[i]=new ms3d_edge_t();
			    _i.arrEdges[i].edgeIndices[0] = (ushort) (((uint)setEdgePair[i]) & 0x0000FFFF);
			    _i.arrEdges[i].edgeIndices[1] = (ushort) ((((uint)setEdgePair[i]) >> 16) & 0xFFFF);
		    }
    	
		    // groups
		    ushort nNumGroups;
		    //fread(&nNumGroups, 1, sizeof(word), fp);
		    nNumGroups=br.ReadUInt16();
		    _i.arrGroups=new ms3d_group_t[nNumGroups];
		    for (i = 0; i < nNumGroups; i++)
		    {
			    _i.arrGroups[i]=new ms3d_group_t();
			    _i.arrGroups[i].Load(br);
		    }
    		

    	
		    // materials
		    ushort nNumMaterials;
		    nNumMaterials=br.ReadUInt16();
		    _i.arrMaterials=new ms3d_material_t[nNumMaterials];
		    for(i=0;i<nNumMaterials;i++)
		    {
			    _i.arrMaterials[i]=new ms3d_material_t();
			    _i.arrMaterials[i].Load(br);
		    }
    	
		    _i.fAnimationFPS=br.ReadSingle();
		    _i.fCurrentTime=br.ReadSingle();
		    _i.iTotalFrames=br.ReadInt32();
    	
		    // joints
		    ushort nNumJoints;
		    nNumJoints=br.ReadUInt16();
		    _i.arrJoints=new ms3d_joint_t[nNumJoints];
		    for (i = 0; i < nNumJoints; i++)
		    {
			    _i.arrJoints[i]=new ms3d_joint_t();
			    _i.arrJoints[i].Load(br);
		    }
    		


    	
		    //br.Close();
		    //fs.Close();
    	
		    return true;
	    }
    	
	    public void Clear()
	    {
		    _i.arrVertices=null;
		    _i.arrTriangles=null;
		    _i.arrEdges=null;
		    _i.arrGroups=null;
		    _i.arrMaterials=null;
		    _i.arrJoints=null;
	    }

	    public int GetNumVertices()
	    {
		    return (int) _i.arrVertices.Length;
	    }
    	
	    public void GetVertexAt(int nIndex, ref ms3d_vertex_t ppVertex)
	    {
		    if (nIndex >= 0 && nIndex < (int) _i.arrVertices.Length)
			    ppVertex = _i.arrVertices[nIndex];
	    }
    	
	    public int GetNumTriangles()
	    {
		    return (int) _i.arrTriangles.Length;
	    }
    	
	    public void GetTriangleAt(int nIndex, ref ms3d_triangle_t ppTriangle)
	    {
		    if (nIndex >= 0 && nIndex < (int) _i.arrTriangles.Length)
			    ppTriangle = _i.arrTriangles[nIndex];
	    }
    	
	    public int GetNumEdges()
	    {
		    return (int) _i.arrEdges.Length;
	    }
    	
	    public void GetEdgeAt(int nIndex, ref ms3d_edge_t ppEdge)
	    {
		    if (nIndex >= 0 && nIndex < (int) _i.arrEdges.Length)
			    ppEdge = _i.arrEdges[nIndex];
	    }
    	
	    public int GetNumGroups()
	    {
		    return (int) _i.arrGroups.Length;
	    }
    	
	    public void GetGroupAt(int nIndex, ref ms3d_group_t ppGroup)
	    {
		    if (nIndex >= 0 && nIndex < (int) _i.arrGroups.Length)
			    ppGroup = _i.arrGroups[nIndex];
	    }
    	
	    public int GetNumMaterials()
	    {
		    return (int) _i.arrMaterials.Length;
	    }
    	
	    public void GetMaterialAt(int nIndex, ref ms3d_material_t ppMaterial)
	    {
		    if (nIndex >= 0 && nIndex < (int) _i.arrMaterials.Length)
			    ppMaterial = _i.arrMaterials[nIndex];
	    }
    	
	    public int GetNumJoints()
	    {
		    return (int) _i.arrJoints.Length;
	    }
    	
	    public void GetJointAt(int nIndex, ref ms3d_joint_t ppJoint)
	    {
		    if (nIndex >= 0 && nIndex < (int) _i.arrJoints.Length)
			    ppJoint = _i.arrJoints[nIndex];
	    }
    	
	    public int FindJointByName(string lpszName)
	    {
		    int i;
		    for (i = 0; i < _i.arrJoints.Length; i++)
		    {
			    if (_i.arrJoints[i].name.CompareTo(lpszName)==0)
				    return i;
		    }
    	
		    return -1;
	    }

	    public float GetAnimationFPS()
	    {
		    return _i.fAnimationFPS;
	    }
    	
	    public float GetCurrentTime()
	    {
		    return _i.fCurrentTime;
	    }
    	
	    public int GetTotalFrames()
	    {
		    return _i.iTotalFrames;
	    }
    	



    	
	    class CMS3DFileI
	    {
		    public ms3d_vertex_t[] arrVertices;
		    public ms3d_triangle_t[] arrTriangles;
		    public ms3d_edge_t[] arrEdges;
		    public ms3d_group_t[] arrGroups;
		    public ms3d_material_t[] arrMaterials;
		    public float fAnimationFPS;
		    public float fCurrentTime;
		    public int iTotalFrames;
		    public ms3d_joint_t[] arrJoints;
    	
		    public CMS3DFileI()
		    {
				    fAnimationFPS=24.0f;
				    fCurrentTime=0.0f;
				    iTotalFrames=0;
		    }
	    }

    /*
    private:
	    CMS3DFile(const CMS3DFile& rhs);
	    CMS3DFile& operator=(const CMS3DFile& rhs);*/
    }


	public class ms3d_header_t
	{
		private char[] buf;
		public string 	id;                                    	// always "MS3D000000"
		public int     version;                                    // 4
		public void Load(BinaryReader br)
		{
			buf=new char[10];
			for(int x=0;x<10;x++)
			{
				buf[x]=(char)br.ReadByte();
			}
			id=new String(buf);
			version=br.ReadInt32();
		}
	}
	
	public class ms3d_vertex_t
	{
	   public byte    flags;                                      // SELECTED | SELECTED2 | HIDDEN
	   public float[]   vertex=new float[3];                                  //
	   public byte    boneId;                                     // -1 = no bone
	   public byte    referenceCount;
	   public void Load(BinaryReader br)
	   {
	   		flags=br.ReadByte();
	   		vertex[0]=br.ReadSingle();
	   		vertex[1]=br.ReadSingle();
	   		vertex[2]=br.ReadSingle();
	   		boneId=br.ReadByte();
	   		referenceCount=br.ReadByte();
	   }
	} 
	
	public class ms3d_triangle_t
	{
	   public ushort    flags;                                      // SELECTED | SELECTED2 | HIDDEN
	   public ushort[]   vertexIndices=new ushort[3];                           //
	   public float[][]   vertexNormals; //[3][3];                        //
	   public float[]   s=new float[3];                                       //
	   public float[]   t=new float[3];                                       //
	   public byte    smoothingGroup;                             // 1 - 32
	   public byte    groupIndex;                                 //
	   public ms3d_triangle_t()
	   {
		   	vertexNormals=new float[3][];
		   	for(int x=0;x<3;x++)
		   		vertexNormals[x]=new float[3];
	   }
	   
	   public void Load(BinaryReader br)
	   {
	   		flags=br.ReadUInt16();
	   		vertexIndices[0]=br.ReadUInt16();
	   		vertexIndices[1]=br.ReadUInt16();
	   		vertexIndices[2]=br.ReadUInt16();
	   		
	   		vertexNormals[0][0]=br.ReadSingle();
	   		vertexNormals[0][1]=br.ReadSingle();
	   		vertexNormals[0][2]=br.ReadSingle();
	   		vertexNormals[1][0]=br.ReadSingle();
	   		vertexNormals[1][1]=br.ReadSingle();
	   		vertexNormals[1][2]=br.ReadSingle();
	   		vertexNormals[2][0]=br.ReadSingle();
	   		vertexNormals[2][1]=br.ReadSingle();
	   		vertexNormals[2][2]=br.ReadSingle();
	   		
	   		s[0]=br.ReadSingle();
	   		s[1]=br.ReadSingle();
	   		s[2]=br.ReadSingle();
	   		
	   		t[0]=br.ReadSingle();
	   		t[1]=br.ReadSingle();
	   		t[2]=br.ReadSingle();
	   		
	   		smoothingGroup=br.ReadByte();
	   		groupIndex=br.ReadByte();
	   }
	}
	
	public class ms3d_edge_t
	{
		public ushort[] edgeIndices=new ushort[2];
	}
	
	public class ms3d_group_t
	{
		private char[] buf;
	   public byte            flags;                              // SELECTED | HIDDEN
	   public string            name;                           //32 bytes
	   public ushort            numtriangles;                       //
	   public ushort[]			triangleIndices;					// the groups group the triangles
	   public sbyte            materialIndex;                      // -1 = no material
	   public void Load(BinaryReader br)
	   {
	   		int x=0;
	   		flags = br.ReadByte();
	   		
	   		buf=new char[32];
			for(x=0;x<32;x++)
				buf[x]=(char)br.ReadByte();
	   		name = new String(buf);
	   		
	   		numtriangles = br.ReadUInt16();
	   		triangleIndices=new ushort[numtriangles];
	   		for(x=0;x<numtriangles;x++)
	   		{
	   			triangleIndices[x]=br.ReadUInt16();
	   		}
	   		materialIndex=br.ReadSByte();
	   }
	}
	
	public class ms3d_material_t
	{
		private char[] buf;
	   public string            name;                           //32 bytes
	   public float[]           ambient=new float[4];                         //
	   public float[]           diffuse=new float[4];                         //
	   public float[]           specular=new float[4];                        //
	   public float[]           emissive=new float[4];                        //
	   public float           shininess;                          // 0.0f - 128.0f
	   public float           transparency;                       // 0.0f - 1.0f
	   public byte            mode;                               // 0, 1, 2 is unused now
	   public string            texture;                        //128bytes texture.bmp
	   public string            alphamap;                       //128bytes alpha.bmp
	   public void Load(BinaryReader br)
	   {
	   		int x;
	   	
	   		buf=new char[32];
			for(x=0;x<32;x++)
				buf[x]=(char)br.ReadByte();
	   		name = new String(buf);

	   		
	   		ambient[0]=br.ReadSingle();
	   		ambient[1]=br.ReadSingle();
	   		ambient[2]=br.ReadSingle();
	   		ambient[3]=br.ReadSingle();
	   		   		
	   		diffuse[0]=br.ReadSingle();
	   		diffuse[1]=br.ReadSingle();
	   		diffuse[2]=br.ReadSingle();
	   		diffuse[3]=br.ReadSingle();
	   		   		
	   		specular[0]=br.ReadSingle();
	   		specular[1]=br.ReadSingle();
	   		specular[2]=br.ReadSingle();
	   		specular[3]=br.ReadSingle();
	   		   		   		
	   		emissive[0]=br.ReadSingle();
	   		emissive[1]=br.ReadSingle();
	   		emissive[2]=br.ReadSingle();
	   		emissive[3]=br.ReadSingle();
	   		
	   		shininess=br.ReadSingle();
	   		transparency=br.ReadSingle();
	   		
	   		mode=br.ReadByte();
	   		
	   		buf=new char[128];
			for(x=0;x<128;x++)
				buf[x]=(char)br.ReadByte();
	   		texture = new String(buf);

	   		buf=new char[128];
			for(x=0;x<128;x++)
				buf[x]=(char)br.ReadByte();
	   		alphamap=new String(buf);
	   }
	}
	
	public class ms3d_keyframe_rot_t
	{
	   public float           time;                               // time in seconds
	   public float[]           rotation=new float[3];                        // x, y, z angles
	   public void Load(BinaryReader br)
	   {
	   		time=br.ReadSingle();
	   		rotation[0]=br.ReadSingle();
	   		rotation[1]=br.ReadSingle();
	   		rotation[2]=br.ReadSingle();
	   }
	}
	
	public class ms3d_keyframe_pos_t
	{
	   public float           time;                               // time in seconds
	   public float[]           position=new float[3];                        // local position
	   public void Load(BinaryReader br)
	   {
	   		time=br.ReadSingle();
	   		position[0]=br.ReadSingle();
	   		position[1]=br.ReadSingle();
	   		position[2]=br.ReadSingle();
	   }
	}
	
	public class ms3d_joint_t
	{
		private char[] buf;
	   public byte            flags;                              // SELECTED | DIRTY
	   public string            name;                           //32 bytes
	   public string            parentName;                     //32 bytes
	   public float[]           rotation=new float[3];                        // local reference matrix
	   public float[]           position=new float[3];
	
	   public ushort            numKeyFramesRot;                    //
	   public ushort            numKeyFramesTrans;                  //
	
	   public ms3d_keyframe_rot_t[] keyFramesRot;      // local animation matrices
	   public ms3d_keyframe_pos_t[] keyFramesTrans;  // local animation matrices
	   
	   public void Load(BinaryReader br)
	   {
	   		int x;
	   		flags = br.ReadByte();
	   		
	   		buf=new char[32];
			for(x=0;x<32;x++)
				buf[x]=(char)br.ReadByte();
	   		name = new String(buf);

	   		buf=new char[32];
			for(x=0;x<32;x++)
				buf[x]=(char)br.ReadByte();
	   		parentName = new String(buf);

	   		
	   		rotation[0]=br.ReadSingle();
	   		rotation[1]=br.ReadSingle();
	   		rotation[2]=br.ReadSingle();
	   		
	   		position[0]=br.ReadSingle();
	   		position[1]=br.ReadSingle();
	   		position[2]=br.ReadSingle();
	   		
	   		numKeyFramesRot=br.ReadUInt16();
	   		numKeyFramesTrans=br.ReadUInt16();
	   		
	   		keyFramesRot=new ms3d_keyframe_rot_t[numKeyFramesRot];
	   		for(x=0;x<numKeyFramesRot;x++)
	   		{
	   			keyFramesRot[x]=new ms3d_keyframe_rot_t();
	   			keyFramesRot[x].Load(br);
	   		}
	   		
	   		keyFramesTrans=new ms3d_keyframe_pos_t[numKeyFramesTrans];
	   		for(x=0;x<numKeyFramesRot;x++)
	   		{
	   			keyFramesTrans[x]=new ms3d_keyframe_pos_t();
	   			keyFramesTrans[x].Load(br);
	   		}
	   		
	   }
	}

}
