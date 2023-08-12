import React, { ReactNode } from 'react';
import { Navbar, NavbarBrand } from 'reactstrap';
import { Link } from 'react-router-dom';

const NavBar = ({ children }: { children?: ReactNode }) => {
    return (
        <header>
            <Navbar className="border-bottom box-shadow mb-3" container light>
                <div className="d-flex align-items-start w-100">
                    <h3 className="koti-title"><Link to="/"><span>KoTi</span></Link></h3>
                    {children && <><h3>&nbsp;&rsaquo;&nbsp;</h3>{children}</>}
                </div>
            </Navbar>
        </header>
    );
};

export default NavBar;
