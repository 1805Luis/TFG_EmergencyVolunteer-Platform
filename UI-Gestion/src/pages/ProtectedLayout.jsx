import { useState } from "react";
import Sidebar from "../components/Sidebar.jsx";
import { Outlet } from "react-router-dom";
import "../styles/ProtectedLayout.css";
import LegalFooter from "../components/LegalFooter.jsx"

export default function ProtectedLayout() {
  const [isExpanded, setIsExpanded] = useState(true);

  const toggleSidebar = () => setIsExpanded(prev => !prev);

  return (
    <div className="layout">
      <Sidebar isExpanded={isExpanded} onToggle={toggleSidebar} />
      <div className={`layout-main ${isExpanded ? "expanded" : "collapsed"}`}>
        <main className="layout-content">
          <Outlet />
        </main>
        <footer className="layout-footer"><LegalFooter/></footer>
      </div>
    </div>
  );
}
