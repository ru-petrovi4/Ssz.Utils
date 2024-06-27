// ObjectManager.h : Declaration of the ObjectManager

#pragma once

#include <vector>
#include <memory>

/// <summary>
/// Manage std::vector of shared ptrs, allows access through handles, designed for effective insertion and deletion
/// </summary>
template<class T>
class ObjectManager
{
public:
	class Iterator
	{
	public:
		Iterator() :
			m_Ptr(nullptr),
			m_CurrentIndex(0)
		{
		}

		Iterator(ObjectManager* ptr, size_t currentIndex) :
			m_Ptr(ptr),
			m_CurrentIndex(currentIndex)
		{
		}

		std::shared_ptr< T > operator*() const
		{
			//assert (m_Ptr != nullptr && m_CurrentIndex != 0);

			return m_Ptr->m_Items[m_CurrentIndex].m_pObj;			
		}

		Iterator& operator++()
		{
			//assert (m_Ptr != nullptr && m_CurrentIndex != 0);
			m_CurrentIndex = m_Ptr->m_Items[m_CurrentIndex].m_NextIndex;
			if (!(m_Ptr->m_Items[m_CurrentIndex].m_pObj)) m_CurrentIndex = 0;
			return (*this);
		}

		Iterator operator++(int)
		{	// postincrement
			Iterator _Tmp = *this;
			++*this;
			return (_Tmp);
		}

		bool operator==(const Iterator& _Right) const
		{	// test for iterator equality			
			return (this->m_Ptr == _Right.m_Ptr && this->m_CurrentIndex == _Right.m_CurrentIndex);
		}

		bool operator!=(const Iterator& _Right) const
		{	// test for iterator inequality
			return (!(*this == _Right));
		}
	private:
		ObjectManager* m_Ptr;
		size_t m_CurrentIndex;
	};

public:

	ObjectManager(void)
	{
		m_Items.push_back(Item());		
		m_Count = 0;
	}

	ObjectManager(size_t count)
	{
		m_Items.reserve(count);
		m_Items.push_back(Item());		
		m_Count = 0;
	}

	~ObjectManager(void) { }

	/// <summary>
	/// Reserve space in internal std::vector of shared ptrs
	/// </summary>
	void Reserve(size_t count)
	{
		m_Items.reserve(count);		
	}
	
	/// <summary>
	/// Puts shared ptr in internal std::vector and returns its handle there	
	/// </summary>
	unsigned Put(std::shared_ptr< T >& pObj)
	{
		if (!pObj) return 0;
		return IndexToHandle(PutInternal(pObj));
	}

	/// <summary>
	/// Gets shared ptr from internal std::vector	
	/// </summary>
	std::shared_ptr< T > Get(unsigned handle)
	{
		size_t index = HandleToIndex(handle);
		if (index == 0) return std::shared_ptr< T >();
		else return GetInternal(index);
	}

	/// <summary>
	/// Releases object from shared ptr from internal std::vector. Its handle becomes invalid	
	/// </summary>
	bool Reset(unsigned handle)
	{
		size_t index = HandleToIndex(handle);
		if (index == 0) return false;
		ResetInternal(index);
        return true;
	}	

	/// <summary>
	/// Releases all objects from shared ptrs from internal std::vector. Its handles and iterators become invalid
	/// </summary>
	void Clear()
	{
		if (m_Items.size() == 1) return;
		m_Items[0].m_NextIndex = 0;
		m_Items[0].m_PrevIndex = 1;
		for (size_t index = 1; index < m_Items.size(); index++)
		{
			m_Items[index].m_pObj.reset();
			m_Items[index].m_NextIndex = index - 1;
			m_Items[index].m_PrevIndex = index + 1 < m_Items.size() ? index + 1 : 0;			
		}
		m_Count = 0;		
	}	

	size_t Count() { return m_Count; }

	Iterator Begin()
	{
		if (m_Count == 0) return Iterator(this, 0);
		return Iterator(this, m_Items[0].m_NextIndex);
	}
	
	Iterator End()
	{
		return Iterator(this, 0);
	}

	std::vector< std::shared_ptr< T > > ToVector()
	{
		std::vector< std::shared_ptr< T > > result;
		result.reserve(Count());

		for (auto it = Begin(); it != End(); it++)
		{
			result.push_back(*it);
		}

		return result;
	}
	
private:
	struct Item
	{
	public:
		Item() : m_InstanceId(0), m_NextIndex(0), m_PrevIndex(0) { }
		Item(Item const & other) : m_pObj(other.m_pObj), m_InstanceId(other.m_InstanceId), m_NextIndex(other.m_NextIndex), m_PrevIndex(other.m_PrevIndex) { }
		Item& operator =(Item const & right)
		{
			m_pObj = right.m_pObj;
			m_InstanceId = right.m_InstanceId;
			m_NextIndex = right.m_NextIndex;
			m_PrevIndex = right.m_PrevIndex;
			return *this;
		}
		std::shared_ptr< T > m_pObj;
		unsigned char m_InstanceId;
		size_t m_NextIndex;		
		size_t m_PrevIndex;
	};

	/// <summary>
	/// Makes instance-specific handle from index
	/// Preconditions: index must be valid
	/// </summary>
	unsigned IndexToHandle(size_t index)
	{
		return ((unsigned)m_Items[index].m_InstanceId << 24) + (unsigned)index;
	}

	/// <summary>
	/// Verifyes that handle corresponds instance of object and returns valid index of shared ptr otherwise (invalid hadler) returns 0
	/// </summary>
	size_t HandleToIndex(unsigned handle)
	{
		if (handle == 0) return 0;
		char id = handle >> 24;
		size_t index = handle - ((unsigned)id << 24);
		if (index >= m_Items.size()) return 0;
		if (m_Items[index].m_pObj.get() == NULL) return 0;
		if (id != m_Items[index].m_InstanceId) return 0;
		return index;
	}

	/// <summary>
	/// Puts shared ptr in internal std::vector and returns its index there
	/// Preconditions: pObj != NULL
	/// </summary>
	size_t PutInternal(std::shared_ptr< T >& pObj)
	{		
		if (m_Items[0].m_PrevIndex == 0)
		{
			m_Items.push_back(Item());
			m_Items[0].m_PrevIndex = m_Items.size() - 1;			
		}

		int index = m_Items[0].m_PrevIndex;
		int nextIndex = m_Items[index].m_NextIndex;
		int prevIndex = m_Items[index].m_PrevIndex;
		int nextIndex0 = m_Items[0].m_NextIndex;
		int prevIndex0 = m_Items[0].m_PrevIndex;		

		m_Items[index].m_pObj = pObj;
		if (m_Items[index].m_InstanceId == 0xFF) m_Items[index].m_InstanceId = 1;
		else m_Items[index].m_InstanceId = m_Items[index].m_InstanceId + 1;		
		m_Items[index].m_NextIndex = nextIndex0;
		m_Items[index].m_PrevIndex = 0;

		m_Items[0].m_NextIndex = index;
		m_Items[0].m_PrevIndex = prevIndex;

		if (prevIndex > 0) m_Items[prevIndex].m_NextIndex = 0;

		if (nextIndex0 > 0) m_Items[nextIndex0].m_PrevIndex = index; 		
		
		m_Count++;

		return index;
	}

	/// <summary>
	/// Gets shared ptr from internal std::vector
	/// Preconditions: index must be valid
	/// </summary>
	std::shared_ptr< T > GetInternal(size_t index)
	{
		return m_Items[index].m_pObj;
	}

	/// <summary>
	/// Releases object from shared ptr from internal std::vector. Its handle becomes invalid
	/// Preconditions: index must be valid
	/// </summary>
	void ResetInternal(size_t index)
	{
		int nextIndex = m_Items[index].m_NextIndex;
		int prevIndex = m_Items[index].m_PrevIndex;
		int nextIndex0 = m_Items[0].m_NextIndex;
		int prevIndex0 = m_Items[0].m_PrevIndex;

		m_Items[index].m_pObj.reset();
		m_Items[index].m_NextIndex = 0;
		m_Items[index].m_PrevIndex = prevIndex0;

		m_Items[0].m_PrevIndex = index;

		if (prevIndex0 > 0) m_Items[prevIndex0].m_NextIndex = index;

		m_Items[prevIndex].m_NextIndex = nextIndex;

		if (nextIndex > 0) m_Items[nextIndex].m_PrevIndex = prevIndex;

		m_Count--;
	}

	std::vector< Item > m_Items;	
	
	size_t			m_Count;	
	size_t			m_CurrentIndex;
};
