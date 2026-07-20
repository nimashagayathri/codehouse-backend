const fs = require('fs');
const path = 'C:\\Users\\This PC\\OneDrive\\Desktop\\codehouse-frontend\\src\\pages\\AdminDashboard.jsx';
let content = fs.readFileSync(path, 'utf8');

content = content.replace(`import Sidebar from '../components/Sidebar';`, `import { useState, useEffect } from 'react';\nimport Sidebar from '../components/Sidebar';\n\nconst API_URL = 'http://localhost:5223';\n`);

const stateAndFetch = `
  const [users, setUsers] = useState([]);
  const token = localStorage.getItem('token');

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    try {
      const response = await fetch(API_URL + '/api/admin/users', {
        headers: { Authorization: '\\u0042earer ' + token }
      });
      const data = await response.json();
      if (Array.isArray(data)) setUsers(data);
    } catch (err) {
      console.error(err);
    }
  };

  const toggleUserStatus = async (id, isActive) => {
    try {
      await fetch(API_URL + '/api/admin/users/' + id + '/status', {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: '\\u0042earer ' + token
        },
        body: JSON.stringify({ isActive: !isActive })
      });
      fetchUsers();
    } catch (err) {
      console.error(err);
    }
  };
`;
content = content.replace(`function AdminDashboard() {\n  return (`, `function AdminDashboard() {\n` + stateAndFetch + `\n  return (`);

content = content.replace(`>245</h3>`, `>{users.length > 0 ? users.length : 245}</h3>`);

const newTbody = `              <tbody>
                {users.map(user => (
                  <tr key={user.id} className="border-b border-slate-50">
                    <td className="py-3 text-slate-700 font-medium">{user.fullName || user.email}</td>
                    <td className="text-slate-500">{user.email}</td>
                    <td className="text-slate-500">{user.role}</td>
                    <td>
                      <span className={\`px-2 md:px-3 py-1 rounded-full text-xs font-semibold \${user.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}\`}>
                        {user.isActive ? 'Active' : 'Disabled'}
                      </span>
                    </td>
                    <td>
                      <button 
                        onClick={() => toggleUserStatus(user.id, user.isActive)}
                        className={\`text-white px-2 md:px-3 py-1 rounded-lg text-xs md:text-sm \${user.isActive ? 'bg-red-500 hover:bg-red-600' : 'bg-green-500 hover:bg-green-600'}\`}>
                        {user.isActive ? 'Disable' : 'Enable'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>`;

content = content.replace(/<tbody>[\s\S]*?<\/tbody>/, newTbody);

fs.writeFileSync(path, content, 'utf8');
